using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TreeCutting.Api.Dtos;
using TreeCutting.Api.Services;

namespace TreeCutting.Api;

public static class CitizenEndpointExtensions
{
    public static IEndpointRouteBuilder MapCitizenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/citizen").RequireAuthorization("CitizenOnly");

        group.MapGet("/profile", (HttpContext context) =>
        {
            var citizenId = GetCitizenId(context.User);
            return citizenId is null
                ? Results.Unauthorized()
                : Results.Ok(new CitizenProfileDto(
                    citizenId,
                    GetClaim(context.User, "name", "full_name") ?? string.Empty,
                    GetClaim(context.User, "mobile_number", "phone_number", ClaimTypes.MobilePhone) ?? string.Empty,
                    GetClaim(context.User, "email", ClaimTypes.Email) ?? string.Empty,
                    GetClaim(context.User, "address") ?? string.Empty));
        });

        group.MapGet("/applications", async (HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            var applications = await service.GetCitizenApplicationsAsync(citizenId);
            return Results.Ok(applications.Select(application => application with
            {
                Status = MapCitizenStatus(application.Status)
            }));
        });

        group.MapGet("/applications/{applicationId:int}", async (int applicationId, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            var application = await service.GetCitizenApplicationByIdAsync(applicationId, citizenId);
            return application is null
                ? Results.NotFound()
                : Results.Ok(application with { Status = MapCitizenStatus(application.Status) });
        });

        group.MapPost("/applications", async (ApplicationCreateRequest request, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            var profileRequest = request with
            {
                FullName = GetClaim(context.User, "name", "full_name") ?? request.FullName,
                EmailId = GetClaim(context.User, "email", ClaimTypes.Email) ?? request.EmailId,
                MobileNo = GetClaim(context.User, "mobile_number", "phone_number", ClaimTypes.MobilePhone) ?? request.MobileNo,
                Address = GetClaim(context.User, "address") ?? request.Address
            };
            var application = await service.CreateApplicationAsync(profileRequest, citizenId);
            return Results.Created($"/api/citizen/applications/{application.ApplicationId}",
                new ApplicationCreateResponse(application.ApplicationId, application.ApplicationNumber));
        });

        group.MapGet("/applications/{applicationId:int}/documents", async (int applicationId, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            if (!await service.CitizenApplicationBelongsToAsync(applicationId, citizenId))
            {
                return Results.NotFound();
            }

            return Results.Ok(await service.GetApplicationDocumentsAsync(applicationId));
        });

        group.MapPost("/applications/{applicationId:int}/documents/upload", async (int applicationId, HttpRequest request, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            if (!await service.CitizenApplicationBelongsToAsync(applicationId, citizenId))
            {
                return Results.NotFound();
            }

            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file is null || !int.TryParse(form["applicationTypeId"], out var applicationTypeId) || !int.TryParse(form["documentTypeId"], out var documentTypeId))
            {
                return Results.BadRequest(new { message = "A file, application type, and document type are required." });
            }

            try
            {
                return Results.Ok(await service.UploadDocumentAsync(applicationId, applicationTypeId, documentTypeId, file));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapDelete("/applications/{applicationId:int}/documents/{documentId:int}", async (int applicationId, int documentId, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            if (!await service.CitizenApplicationBelongsToAsync(applicationId, citizenId))
            {
                return Results.NotFound();
            }

            return await service.DeleteDocumentAsync(applicationId, documentId) ? Results.Ok(new { success = true }) : Results.NotFound();
        });

        group.MapGet("/applications/{applicationId:int}/documents/{documentId:int}/file", async (int applicationId, int documentId, HttpContext context, TreeCuttingService service, IWebHostEnvironment environment) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            var application = await service.GetCitizenApplicationByIdAsync(applicationId, citizenId);
            var document = application?.Documents.SingleOrDefault(x => x.ApplicationDocumentId == documentId);
            return document is null ? Results.NotFound() : FileResult(environment, document.FilePath, document.ContentType, document.FileName);
        });

        group.MapGet("/applications/{applicationId:int}/photos", async (int applicationId, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            var application = await service.GetCitizenApplicationByIdAsync(applicationId, citizenId);
            return application is null ? Results.NotFound() : Results.Ok(application.Photos);
        });

        group.MapPost("/applications/{applicationId:int}/photos/upload", async (int applicationId, HttpRequest request, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            var photoId = int.TryParse(form["photoId"], out var parsedPhotoId) ? parsedPhotoId : (int?)null;
            if (file is null)
            {
                return Results.BadRequest(new { message = "A photo is required." });
            }

            try
            {
                return Results.Ok(await service.UploadPhotoAsync(applicationId, citizenId, photoId, file));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        group.MapDelete("/applications/{applicationId:int}/photos/{photoId:int}", async (int applicationId, int photoId, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            return await service.DeletePhotoAsync(applicationId, citizenId, photoId) ? Results.Ok(new { success = true }) : Results.NotFound();
        });

        group.MapGet("/applications/{applicationId:int}/photos/{photoId:int}/file", async (int applicationId, int photoId, HttpContext context, TreeCuttingService service, IWebHostEnvironment environment) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            var application = await service.GetCitizenApplicationByIdAsync(applicationId, citizenId);
            var photo = application?.Photos.SingleOrDefault(x => x.ApplicationPhotoId == photoId);
            return photo is null ? Results.NotFound() : FileResult(environment, photo.FilePath, photo.ContentType, photo.FileName);
        });

        group.MapPost("/applications/{applicationId:int}/submit", async (int applicationId, HttpContext context, TreeCuttingService service) =>
        {
            var citizenId = GetRequiredCitizenId(context.User);
            var application = await service.GetCitizenApplicationByIdAsync(applicationId, citizenId);
            if (application is null)
            {
                return Results.NotFound();
            }

            if (application.Photos.Count == 0)
            {
                return Results.BadRequest(new { message = "At least one tree photograph is required before submitting the application." });
            }

            try
            {
                var submitted = await service.SubmitApplicationAsync(applicationId);
                return submitted is null ? Results.NotFound() : Results.Ok(submitted with { Status = MapCitizenStatus(submitted.Status) });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        });

        return endpoints;
    }

    private static string GetRequiredCitizenId(ClaimsPrincipal user) => GetCitizenId(user) ?? throw new UnauthorizedAccessException("A citizen identity is required.");

    private static string? GetCitizenId(ClaimsPrincipal user) => GetClaim(user, "citizen_id", "user_id", ClaimTypes.NameIdentifier, "sub");

    private static string? GetClaim(ClaimsPrincipal user, params string[] claimTypes)
    {
        return claimTypes.Select(user.FindFirst).FirstOrDefault(claim => !string.IsNullOrWhiteSpace(claim?.Value))?.Value;
    }

    private static string MapCitizenStatus(string status) => status switch
    {
        "Draft" => "Draft",
        "Submitted" or "PENDING_AGS_INSPECTION" or "PENDING_HOD" => "Under Department Review",
        "PENDING_COMMITTEE" => "Under Committee Review",
        "PENDING_COMMISSIONER" => "Under Final Approval",
        "APPROVED" => "Approved",
        "REJECTED" => "Rejected",
        "RETURNED" => "Action Required",
        _ => "Under Department Review"
    };

    private static IResult FileResult(IWebHostEnvironment environment, string relativePath, string contentType, string fileName)
    {
        var physicalPath = Path.Combine(environment.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(physicalPath) ? Results.File(physicalPath, contentType, fileName, enableRangeProcessing: true) : Results.NotFound();
    }
}
