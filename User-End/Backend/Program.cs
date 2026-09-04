using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TreeCutting.Api.Data;
using TreeCutting.Api.Dtos;
using TreeCutting.Api.Models;
using TreeCutting.Api.Services;
using TreeCutting.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TreeCuttingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\MSSQLLocalDB;Database=TreeCuttingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"));

builder.Services.AddScoped<TreeCuttingService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CitizenOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.Claims.Any(claim =>
            (claim.Type is "role" or "roles" or ClaimTypes.Role)
            && claim.Value.Equals("CITIZEN", StringComparison.OrdinalIgnoreCase)));
    });
});
builder.Services.Configure<SmsGatewayOptions>(builder.Configuration.GetSection("SmsGateway"));
builder.Services.AddHttpClient<ISmsGateway, SmsGateway>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("AllowedOrigins")
        .Get<string[]>() ?? new[] { "http://localhost:3000" };

    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TreeCuttingDbContext>();
    DatabaseBootstrap.InitializeDatabase(db);
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapCitizenEndpoints();

app.MapGet("/api/masters/application-types", async (TreeCuttingService service) =>
    Results.Ok(await service.GetApplicationTypesAsync()));

app.MapGet("/api/masters/applicant-types", async (TreeCuttingService service) =>
    Results.Ok(await service.GetApplicantTypesAsync()));

app.MapGet("/api/masters/zones", async (TreeCuttingService service) =>
    Results.Ok(await service.GetZonesAsync()));

app.MapGet("/api/masters/peths", async (TreeCuttingService service) =>
    Results.Ok(await service.GetPethsAsync()));

app.MapGet("/api/masters/prabhags", async (TreeCuttingService service) =>
    Results.Ok(await service.GetPrabhaksAsync()));

app.MapGet("/api/masters/documents/application-type/{applicationTypeId:int}", async (int applicationTypeId, TreeCuttingService service) =>
    Results.Ok(await service.GetDocumentsByApplicationTypeAsync(applicationTypeId)));

app.MapPost("/api/applications", async ([FromBody] ApplicationCreateRequest request, TreeCuttingService service) =>
{
    try
    {
        var result = await service.CreateApplicationAsync(request);
        var response = new ApplicationCreateResponse(result.ApplicationId, result.ApplicationNumber);
        return Results.Created($"/api/applications/{result.ApplicationId}", response);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapGet("/api/applications/{applicationId:int}", async (int applicationId, TreeCuttingService service) =>
{
    var result = await service.GetApplicationByIdAsync(applicationId);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPut("/api/applications/{applicationId:int}", async (int applicationId, [FromBody] ApplicationUpdateRequest request, TreeCuttingService service) =>
{
    try
    {
        var result = await service.UpdateApplicationAsync(applicationId, request);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/applications/{applicationId:int}/submit", async (int applicationId, TreeCuttingService service) =>
{
    try
    {
        var result = await service.SubmitApplicationAsync(applicationId);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapGet("/api/applications/{applicationId:int}/documents", async (int applicationId, TreeCuttingService service) =>
    Results.Ok(await service.GetApplicationDocumentsAsync(applicationId)));

app.MapPost("/api/applications/{applicationId:int}/documents/upload", async (HttpRequest request, int applicationId, TreeCuttingService service) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var file = form.Files["file"];
        var applicationTypeValue = form["applicationTypeId"];
        var documentTypeValue = form["documentTypeId"];

        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "A valid file is required." });
        }

        if (string.IsNullOrWhiteSpace(applicationTypeValue) || string.IsNullOrWhiteSpace(documentTypeValue))
        {
            return Results.BadRequest(new { message = "Application type and document type are required." });
        }

        if (!int.TryParse(applicationTypeValue, out var applicationTypeId) || !int.TryParse(documentTypeValue, out var documentTypeId))
        {
            return Results.BadRequest(new { message = "Application type and document type must be valid numbers." });
        }

        var result = await service.UploadDocumentAsync(applicationId, applicationTypeId, documentTypeId, file);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapDelete("/api/applications/{applicationId:int}/documents/{documentId:int}", async (int applicationId, int documentId, TreeCuttingService service) =>
{
    var result = await service.DeleteDocumentAsync(applicationId, documentId);
    return result ? Results.Ok(new { success = true }) : Results.NotFound();
});

app.Run();

public static class SeedData
{
    public static void Initialize(TreeCuttingDbContext context)
    {
        if (!context.Database.CanConnect())
        {
            throw new InvalidOperationException("Database is not reachable.");
        }

        if (!context.ApplicationTypes.Any())
        {
            context.ApplicationTypes.AddRange(
                new ApplicationType { ApplicationTypeName = "Cutting Dangerous Branches", IsActive = true },
                new ApplicationType { ApplicationTypeName = "Cutting Dangerous Branches in electric lines", IsActive = true },
                new ApplicationType { ApplicationTypeName = "Cutting Dangerous Branches in building construction", IsActive = true }
            );
        }

        if (!context.ApplicantTypes.Any())
        {
            context.ApplicantTypes.AddRange(
                new ApplicantType { ApplicantTypeName = "Home", IsActive = true },
                new ApplicantType { ApplicantTypeName = "Public Place", IsActive = true },
                new ApplicantType { ApplicantTypeName = "Neighbours", IsActive = true },
                new ApplicantType { ApplicantTypeName = "Others", IsActive = true }
            );
        }

        if (!context.DocumentTypes.Any())
        {
            context.DocumentTypes.AddRange(
                new DocumentType { DocumentTypeName = "Photo of tree", IsActive = true },
                new DocumentType { DocumentTypeName = "Letter of Guarantee", IsActive = true },
                new DocumentType { DocumentTypeName = "Aadhar card", IsActive = true },
                new DocumentType { DocumentTypeName = "Property Tax NOC", IsActive = true },
                new DocumentType { DocumentTypeName = "Photo of the tree from all angles", IsActive = true },
                new DocumentType { DocumentTypeName = "Construction permission", IsActive = true },
                new DocumentType { DocumentTypeName = "SMC layout", IsActive = true },
                new DocumentType { DocumentTypeName = "Photo of the tree", IsActive = true }
            );
        }

        context.SaveChanges();

        var applicationTypes = context.ApplicationTypes.ToList();
        var documentTypes = context.DocumentTypes.ToList();

        foreach (var applicationType in applicationTypes)
        {
            if (context.ApplicationTypeDocumentMappings.Any(x => x.ApplicationTypeId == applicationType.ApplicationTypeId))
            {
                continue;
            }

            var requiredDocumentNames = applicationType.ApplicationTypeName switch
            {
                "Cutting Dangerous Branches" => new[]
                {
                    "Photo of tree",
                    "Letter of Guarantee",
                    "Aadhar card",
                    "Property Tax NOC",
                    "Photo of the tree from all angles"
                },
                "Cutting Dangerous Branches in electric lines" => new[]
                {
                    "Photo of tree",
                    "Letter of Guarantee",
                    "Aadhar card",
                    "Property Tax NOC",
                    "Photo of the tree from all angles"
                },
                "Cutting Dangerous Branches in building construction" => new[]
                {
                    "Photo of the tree",
                    "Letter of Guarantee",
                    "Aadhar card",
                    "Construction permission",
                    "SMC layout",
                    "Property Tax NOC",
                    "Photo of the tree from all angles"
                },
                _ => Array.Empty<string>()
            };

            foreach (var (name, index) in requiredDocumentNames.Select((name, index) => (name, index)))
            {
                var documentType = documentTypes.Single(x => x.DocumentTypeName == name);
                context.ApplicationTypeDocumentMappings.Add(new ApplicationTypeDocumentMapping
                {
                    ApplicationTypeId = applicationType.ApplicationTypeId,
                    DocumentTypeId = documentType.DocumentTypeId,
                    IsRequired = true,
                    DisplayOrder = index + 1
                });
            }
        }

        context.SaveChanges();
    }
}

public static class DatabaseBootstrap
{
    public static void InitializeDatabase(TreeCuttingDbContext context)
    {
        var expectedTables = new[]
        {
            "ApplicationType",
            "ApplicantType",
            "DocumentType",
            "ApplicationTypeDocumentMapping",
            "Application",
            "ApplicationDocument",
            "ApplicationPhoto"
        };

        try
        {
            var tableNames = context.Database.SqlQueryRaw<string>(
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo'").ToList();

            var missingTables = expectedTables
                .Except(tableNames, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingTables.Count > 0)
            {
                context.Database.EnsureCreated();
            }

            context.Database.ExecuteSqlRaw("IF COL_LENGTH('dbo.Application', 'SmsSent') IS NULL ALTER TABLE dbo.Application ADD SmsSent bit NOT NULL CONSTRAINT DF_Application_SmsSent DEFAULT 0 WITH VALUES;");
            context.Database.ExecuteSqlRaw("IF COL_LENGTH('dbo.Application', 'SmsSentDate') IS NULL ALTER TABLE dbo.Application ADD SmsSentDate datetime2 NULL;");
            context.Database.ExecuteSqlRaw("IF COL_LENGTH('dbo.Application', 'SmsError') IS NULL ALTER TABLE dbo.Application ADD SmsError nvarchar(500) NULL;");
            context.Database.ExecuteSqlRaw("IF COL_LENGTH('dbo.Application', 'CitizenId') IS NULL ALTER TABLE dbo.Application ADD CitizenId nvarchar(200) NULL;");
            context.Database.ExecuteSqlRaw("IF OBJECT_ID('dbo.ApplicationPhoto', 'U') IS NULL CREATE TABLE dbo.ApplicationPhoto (ApplicationPhotoId int IDENTITY(1,1) NOT NULL PRIMARY KEY, ApplicationId int NOT NULL, FileName nvarchar(255) NOT NULL, FilePath nvarchar(500) NOT NULL, ContentType nvarchar(200) NULL, UploadedDate datetime2 NOT NULL CONSTRAINT DF_ApplicationPhoto_UploadedDate DEFAULT SYSUTCDATETIME());");
            context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Application_CitizenId' AND object_id = OBJECT_ID('dbo.Application')) CREATE INDEX IX_Application_CitizenId ON dbo.Application (CitizenId);");
            context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApplicationPhoto_ApplicationId' AND object_id = OBJECT_ID('dbo.ApplicationPhoto')) CREATE INDEX IX_ApplicationPhoto_ApplicationId ON dbo.ApplicationPhoto (ApplicationId);");

            SeedData.Initialize(context);
        }
        catch
        {
            context.Database.EnsureCreated();
            SeedData.Initialize(context);
        }
    }
}

public partial class Program
{
}

