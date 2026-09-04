using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using AdminPanel.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AdminDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("Development").AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>("Development", _ => { });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = true;
    });
}
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});
builder.Services.AddCors(options => options.AddPolicy("AdminFrontend", policy => policy
    .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3001"])
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
var app = builder.Build();
app.UseCors("AdminFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapAdminEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/admin/test-login/options", async (AdminDbContext db) => Results.Ok(await db.Roles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.RoleId).Select(x => new { code = x.Code, name = x.Name }).ToListAsync())).AllowAnonymous();
    app.MapPost("/api/admin/test-login", async (TestLoginRequest request, HttpContext context, AdminDbContext db) =>
    {
        var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive && x.Code == request.Role);
        if (role is null) return Results.BadRequest(new { message = "This test role is not configured." });
        var employeeId = $"test-{role.Code.ToLowerInvariant()}";
        context.Response.Cookies.Append("smc_admin_test_employee", employeeId, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true });
        context.Response.Cookies.Append("smc_admin_test_role", role.Code, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true });
        return Results.Ok(new { role = role.Code, employeeId });
    }).AllowAnonymous();
}
app.MapGet("/api/admin/session", async (HttpContext context, AdminDbContext db) => Results.Ok(new
{
    Name = context.User.FindFirst("name")?.Value ?? context.User.FindFirst(ClaimTypes.Name)?.Value,
    Role = await AdminEndpoints.GetCurrentRoleAsync(context.User, db),
    EmployeeId = context.User.FindFirstValue("employee_id") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub")
})).RequireAuthorization("AdminOnly");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.TreeCuttingWorkflowHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TreeCuttingWorkflowHistory (WorkflowId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, ApplicationId INT NOT NULL, FromRole NVARCHAR(50) NOT NULL, ToRole NVARCHAR(50) NOT NULL, Action NVARCHAR(100) NOT NULL, PreviousStatus NVARCHAR(100) NOT NULL, NewStatus NVARCHAR(100) NOT NULL, Remarks NVARCHAR(2000) NULL, ActionBy NVARCHAR(200) NOT NULL, ActionDate DATETIME2 NOT NULL);
    CREATE INDEX IX_TreeCuttingWorkflowHistory_ApplicationId ON dbo.TreeCuttingWorkflowHistory(ApplicationId);
END
IF OBJECT_ID(N'dbo.TreeCuttingDepartmentReview', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TreeCuttingDepartmentReview (DepartmentReviewId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, ApplicationId INT NOT NULL, Role NVARCHAR(50) NOT NULL, ReviewDate DATETIME2 NOT NULL, InspectionDate DATETIME2 NULL, InspectionFindings NVARCHAR(2000) NULL, Remarks NVARCHAR(2000) NOT NULL, Recommendation NVARCHAR(2000) NOT NULL, ConstructionRelated BIT NULL, Decision NVARCHAR(50) NULL, ActionBy NVARCHAR(200) NOT NULL);
    CREATE INDEX IX_TreeCuttingDepartmentReview_ApplicationId ON dbo.TreeCuttingDepartmentReview(ApplicationId);
END
IF OBJECT_ID(N'dbo.TreeCuttingDepartment', N'U') IS NULL CREATE TABLE dbo.TreeCuttingDepartment (DepartmentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, Name NVARCHAR(200) NOT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingDesignation', N'U') IS NULL CREATE TABLE dbo.TreeCuttingDesignation (DesignationId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, Name NVARCHAR(200) NOT NULL, DepartmentId INT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingRole', N'U') IS NULL CREATE TABLE dbo.TreeCuttingRole (RoleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, Name NVARCHAR(200) NOT NULL, DepartmentId INT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingEmployeeRole', N'U') IS NULL CREATE TABLE dbo.TreeCuttingEmployeeRole (EmployeeRoleId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, EmployeeId NVARCHAR(200) NOT NULL, RoleId INT NOT NULL, DesignationId INT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingWorkflowStatus', N'U') IS NULL CREATE TABLE dbo.TreeCuttingWorkflowStatus (WorkflowStatusId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, Name NVARCHAR(200) NOT NULL, IsTerminal BIT NOT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingWorkflowStage', N'U') IS NULL CREATE TABLE dbo.TreeCuttingWorkflowStage (WorkflowStageId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, RoleId INT NOT NULL, PendingStatusId INT NOT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingWorkflowTransition', N'U') IS NULL CREATE TABLE dbo.TreeCuttingWorkflowTransition (WorkflowTransitionId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY, FromStatusId INT NOT NULL, FromRoleId INT NOT NULL, ActionCode NVARCHAR(100) NOT NULL, ToStatusId INT NOT NULL, ConditionField NVARCHAR(100) NULL, ConditionValue NVARCHAR(200) NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingWorkflowDecision', N'U') IS NULL CREATE TABLE dbo.TreeCuttingWorkflowDecision (WorkflowDecisionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, Name NVARCHAR(200) NOT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingZone', N'U') IS NULL CREATE TABLE dbo.TreeCuttingZone (ZoneId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, Name NVARCHAR(200) NOT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingPrabhag', N'U') IS NULL CREATE TABLE dbo.TreeCuttingPrabhag (PrabhagId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, Name NVARCHAR(200) NOT NULL, ZoneId INT NULL, IsActive BIT NOT NULL);
IF OBJECT_ID(N'dbo.TreeCuttingWard', N'U') IS NULL CREATE TABLE dbo.TreeCuttingWard (WardId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, Code NVARCHAR(100) NOT NULL UNIQUE, Name NVARCHAR(200) NOT NULL, PrabhagId INT NULL, IsActive BIT NOT NULL);
""");
    if (app.Environment.IsDevelopment())
    {
        await db.Database.ExecuteSqlRawAsync("""
INSERT dbo.TreeCuttingEmployeeRole (EmployeeId, RoleId, IsActive)
SELECT CONCAT('test-', LOWER(r.Code)), r.RoleId, 1
FROM dbo.TreeCuttingRole r
WHERE r.IsActive = 1
AND NOT EXISTS (SELECT 1 FROM dbo.TreeCuttingEmployeeRole er WHERE er.EmployeeId = CONCAT('test-', LOWER(r.Code)) AND er.RoleId = r.RoleId);
""");
    }
}
app.Run();

public partial class Program { }

public record TestLoginRequest(string Role);
