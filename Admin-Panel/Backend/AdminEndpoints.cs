using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AdminPanel.Api.Models;

namespace AdminPanel.Api;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin/tree-cutting").RequireAuthorization("AdminOnly");
        group.MapGet("/masters/application-types", (AdminDbContext db) => db.ApplicationTypes.AsNoTracking().OrderBy(x => x.ApplicationTypeName).Select(x => new { id = x.ApplicationTypeId, name = x.ApplicationTypeName, x.IsActive }).ToListAsync());
        group.MapPost("/masters/application-types", (MasterRequest request, AdminDbContext db) => CreateMaster(request.Name, db.ApplicationTypes, db));
        group.MapPut("/masters/application-types/{id:int}", (int id, MasterRequest request, AdminDbContext db) => UpdateApplicationType(id, request, db));
        group.MapDelete("/masters/application-types/{id:int}", (int id, AdminDbContext db) => DeactivateApplicationType(id, db));
        group.MapGet("/masters/applicant-types", (AdminDbContext db) => db.ApplicantTypes.AsNoTracking().OrderBy(x => x.ApplicantTypeName).Select(x => new { id = x.ApplicantTypeId, name = x.ApplicantTypeName, x.IsActive }).ToListAsync());
        group.MapPost("/masters/applicant-types", (MasterRequest request, AdminDbContext db) => CreateApplicantType(request, db));
        group.MapPut("/masters/applicant-types/{id:int}", (int id, MasterRequest request, AdminDbContext db) => UpdateApplicantType(id, request, db));
        group.MapDelete("/masters/applicant-types/{id:int}", (int id, AdminDbContext db) => DeactivateApplicantType(id, db));
        group.MapGet("/masters/document-types", (AdminDbContext db) => db.DocumentTypes.AsNoTracking().OrderBy(x => x.DocumentTypeName).Select(x => new { id = x.DocumentTypeId, name = x.DocumentTypeName, x.IsActive }).ToListAsync());
        group.MapPost("/masters/document-types", (MasterRequest request, AdminDbContext db) => CreateDocumentType(request, db));
        group.MapPut("/masters/document-types/{id:int}", (int id, MasterRequest request, AdminDbContext db) => UpdateDocumentType(id, request, db));
        group.MapDelete("/masters/document-types/{id:int}", (int id, AdminDbContext db) => DeactivateDocumentType(id, db));
        group.MapGet("/masters/decisions", (AdminDbContext db) => db.WorkflowDecisions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { id = x.WorkflowDecisionId, code = x.Code, name = x.Name }).ToListAsync());
        group.MapGet("/configuration", GetConfiguration);
        group.MapGet("/applications", ListApplications);
        group.MapGet("/applications/{applicationId:int}", GetApplication);
        group.MapGet("/applications/{applicationId:int}/workflow", GetWorkflow);
        group.MapGet("/applications/{applicationId:int}/documents/{documentId:int}/file", GetDocumentFile);
        group.MapGet("/applications/{applicationId:int}/photos/{photoId:int}/file", GetPhotoFile);
        group.MapPost("/applications/{applicationId:int}/inspection", (int applicationId, InspectionRequest request, ClaimsPrincipal user, AdminDbContext db) => Process(applicationId, "AGS", request, user, db));
        group.MapPost("/applications/{applicationId:int}/construction-review", (int applicationId, ConstructionReviewRequest request, ClaimsPrincipal user, AdminDbContext db) => Process(applicationId, "NAGAR_ABHIYANTA", request, user, db));
        group.MapPost("/applications/{applicationId:int}/hod-review", (int applicationId, ReviewRequest request, ClaimsPrincipal user, AdminDbContext db) => Process(applicationId, "HOD_GARDEN", request, user, db));
        group.MapPost("/applications/{applicationId:int}/committee-review", (int applicationId, DecisionRequest request, ClaimsPrincipal user, AdminDbContext db) => Process(applicationId, "COMMITTEE", request, user, db));
        group.MapPost("/applications/{applicationId:int}/commissioner-review", (int applicationId, DecisionRequest request, ClaimsPrincipal user, AdminDbContext db) => Process(applicationId, "COMMISSIONER", request, user, db));
    }

    private static async Task<IResult> ListApplications(HttpRequest request, ClaimsPrincipal user, AdminDbContext db)
    {
        var role = await GetCurrentRoleAsync(user, db);
        if (role is null) return Results.Forbid();
        var roleId = await db.Roles.Where(x => x.IsActive && x.Code == role).Select(x => x.RoleId).SingleOrDefaultAsync();
        var stage = await db.WorkflowStages.Where(x => x.IsActive && x.RoleId == roleId).OrderBy(x => x.WorkflowStageId).Select(x => new { x.WorkflowStageId, x.PendingStatusId }).FirstOrDefaultAsync();
        var status = stage is null ? null : await db.WorkflowStatuses.Where(x => x.IsActive && x.WorkflowStatusId == stage.PendingStatusId).Select(x => x.Code).SingleOrDefaultAsync();
        if (status is null) return Results.Forbid();
        var firstStageRoleId = await db.WorkflowStages.Where(x => x.IsActive).OrderBy(x => x.WorkflowStageId).Select(x => x.RoleId).FirstOrDefaultAsync();
        var query = roleId == firstStageRoleId ? db.Applications.AsNoTracking().Where(x => x.Status == status || x.Status == "Submitted") : db.Applications.AsNoTracking().Where(x => x.Status == status);
        var search = request.Query["search"].ToString().Trim();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.ApplicationNumber.Contains(search) || x.FullName.Contains(search) || x.MobileNo.Contains(search));
        var total = await query.CountAsync();
        var page = Math.Max(1, int.TryParse(request.Query["page"], out var parsedPage) ? parsedPage : 1);
        var pageSize = Math.Clamp(int.TryParse(request.Query["pageSize"], out var parsedSize) ? parsedSize : 25, 1, 100);
        var rows = await query.OrderBy(x => x.SubmittedDate ?? x.CreatedDate).Skip((page - 1) * pageSize).Take(pageSize)
            .Join(db.ApplicationTypes, application => application.ApplicationTypeId, type => type.ApplicationTypeId, (application, type) => new { application, type.ApplicationTypeName })
            .Select(x => new { x.application.ApplicationId, x.application.ApplicationNumber, applicantName = x.application.FullName, applicantMobile = x.application.MobileNo, applicationDate = x.application.SubmittedDate ?? x.application.CreatedDate, applicationType = x.ApplicationTypeName, x.application.ZoneNo, x.application.PrabhagNo, x.application.PethNo, propertyDetails = x.application.PropertyTaxNo, x.application.Status, pendingSince = x.application.UpdatedDate ?? x.application.SubmittedDate ?? x.application.CreatedDate, responsibleRole = role }).ToListAsync();
        return Results.Ok(new { items = rows, total, page, pageSize, role, pendingStatus = status });
    }

    private static async Task<IResult> GetApplication(int applicationId, ClaimsPrincipal user, AdminDbContext db)
    {
        var application = await db.Applications.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationId == applicationId);
        if (application is null) return Results.NotFound();
        var docs = await db.ApplicationDocuments.AsNoTracking().Where(x => x.ApplicationId == applicationId).Join(db.DocumentTypes, d => d.DocumentTypeId, t => t.DocumentTypeId, (d, t) => new { d.ApplicationDocumentId, d.DocumentTypeId, documentType = t.DocumentTypeName, d.FileName, d.FilePath, d.ContentType, d.UploadedDate }).ToListAsync();
        var photos = await db.ApplicationPhotos.AsNoTracking().Where(x => x.ApplicationId == applicationId).Select(x => new { x.ApplicationPhotoId, x.FileName, x.FilePath, x.ContentType, x.UploadedDate }).ToListAsync();
        var reviews = await db.DepartmentReviews.AsNoTracking().Where(x => x.ApplicationId == applicationId).OrderBy(x => x.ReviewDate).ToListAsync();
        return Results.Ok(new { application, documents = docs, photos, reviews, currentUserRole = await GetCurrentRoleAsync(user, db) });
    }

    private static async Task<IResult> GetWorkflow(int applicationId, AdminDbContext db) => Results.Ok(await db.WorkflowHistory.AsNoTracking().Where(x => x.ApplicationId == applicationId).OrderBy(x => x.ActionDate).ToListAsync());

    private static async Task<IResult> GetConfiguration(AdminDbContext db)
    {
        var roles = await db.Roles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.RoleId).Select(x => new { x.RoleId, code = x.Code, name = x.Name }).ToListAsync();
        var statuses = await db.WorkflowStatuses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.WorkflowStatusId).Select(x => new { x.WorkflowStatusId, code = x.Code, name = x.Name, x.IsTerminal }).ToListAsync();
        var stages = await db.WorkflowStages.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.WorkflowStageId).Select(x => new { x.WorkflowStageId, x.Code, x.RoleId, x.PendingStatusId }).ToListAsync();
        var transitions = await db.WorkflowTransitions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.WorkflowTransitionId).Select(x => new { x.WorkflowTransitionId, x.FromStatusId, x.FromRoleId, x.ActionCode, x.ToStatusId, x.ConditionField, x.ConditionValue }).ToListAsync();
        var decisions = await db.WorkflowDecisions.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.WorkflowDecisionId).Select(x => new { x.WorkflowDecisionId, x.Code, x.Name }).ToListAsync();
        return Results.Ok(new { roles, statuses, stages, transitions, decisions });
    }

    private static async Task<IResult> Process(int applicationId, string expectedRole, ReviewRequestBase request, ClaimsPrincipal user, AdminDbContext db)
    {
        var role = await GetCurrentRoleAsync(user, db);
        if (!string.Equals(role, expectedRole, StringComparison.OrdinalIgnoreCase)) return Results.Json(new { message = "You are not authorized to process this application." }, statusCode: StatusCodes.Status403Forbidden);
        var roleId = await db.Roles.Where(x => x.IsActive && x.Code == expectedRole).Select(x => x.RoleId).SingleOrDefaultAsync();
        var expectedStatus = await db.WorkflowStages.Where(x => x.IsActive && x.RoleId == roleId).OrderBy(x => x.WorkflowStageId).Join(db.WorkflowStatuses.Where(x => x.IsActive), configuredStage => configuredStage.PendingStatusId, workflowStatus => workflowStatus.WorkflowStatusId, (_, workflowStatus) => workflowStatus.Code).FirstOrDefaultAsync();
        if (expectedStatus is null) return Results.BadRequest(new { message = "The configured workflow stage is unavailable." });
        if (string.IsNullOrWhiteSpace(request.Remarks) || string.IsNullOrWhiteSpace(request.Recommendation)) return Results.BadRequest(new { message = "Please complete all required fields." });
        var application = await db.Applications.SingleOrDefaultAsync(x => x.ApplicationId == applicationId);
        if (application is null) return Results.NotFound();
        var firstStageRoleId = await db.WorkflowStages.Where(x => x.IsActive).OrderBy(x => x.WorkflowStageId).Select(x => x.RoleId).FirstOrDefaultAsync();
        var isCitizenSubmission = roleId == firstStageRoleId && string.Equals(application.Status, "Submitted", StringComparison.OrdinalIgnoreCase);
        if (!isCitizenSubmission && !string.Equals(application.Status, expectedStatus, StringComparison.OrdinalIgnoreCase)) return Results.Conflict(new { message = "This application is not currently pending for your role." });
        if (expectedRole == "NAGAR_ABHIYANTA" && request is not ConstructionReviewRequest) return Results.BadRequest(new { message = "Construction review data is required." });
        var statusId = await db.WorkflowStatuses.Where(x => x.IsActive && x.Code == application.Status).Select(x => x.WorkflowStatusId).SingleOrDefaultAsync();
        if (statusId == 0) return Results.BadRequest(new { message = "The application status is not configured in workflow statuses." });
        var actionCode = request is DecisionRequest decisionRequest ? decisionRequest.Decision : "SUBMIT";
        var conditionValue = request is InspectionRequest inspectionCondition ? inspectionCondition.ConstructionRelated.ToString().ToLowerInvariant() : null;
        var transition = await db.WorkflowTransitions.Where(x => x.IsActive && x.FromRoleId == roleId && x.FromStatusId == statusId && x.ActionCode == actionCode && (x.ConditionValue == null || x.ConditionValue == conditionValue)).OrderByDescending(x => x.ConditionValue != null).FirstOrDefaultAsync();
        if (transition is null) return Results.BadRequest(new { message = "No valid workflow transition is configured for this action." });
        var nextStatus = await db.WorkflowStatuses.Where(x => x.IsActive && x.WorkflowStatusId == transition.ToStatusId).Select(x => x.Code).SingleAsync();
        var nextRole = await db.WorkflowStages.Where(x => x.IsActive && x.PendingStatusId == transition.ToStatusId).Join(db.Roles.Where(x => x.IsActive), stage => stage.RoleId, configuredRole => configuredRole.RoleId, (_, configuredRole) => configuredRole.Code).SingleOrDefaultAsync() ?? nextStatus;
        await using var transaction = await db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;
        db.DepartmentReviews.Add(new DepartmentReview { ApplicationId = applicationId, Role = expectedRole, ReviewDate = now, InspectionDate = request is InspectionRequest inspectionRequest ? inspectionRequest.InspectionDate : null, InspectionFindings = request is InspectionRequest inspectionReport ? inspectionReport.InspectionFindings : null, Remarks = request.Remarks.Trim(), Recommendation = request.Recommendation.Trim(), ConstructionRelated = request is InspectionRequest constructionReport ? constructionReport.ConstructionRelated : null, Decision = request is DecisionRequest decisionReport ? decisionReport.Decision : null, ActionBy = UserId(user) });
        db.WorkflowHistory.Add(new WorkflowHistory { ApplicationId = applicationId, FromRole = isCitizenSubmission ? "CITIZEN" : expectedRole, ToRole = nextRole, Action = actionCode, PreviousStatus = application.Status, NewStatus = nextStatus, Remarks = request.Remarks.Trim(), ActionBy = UserId(user), ActionDate = now });
        application.Status = nextStatus;
        application.UpdatedDate = now;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Results.Ok(new { application.ApplicationId, application.Status });
    }

    public static async Task<string?> GetCurrentRoleAsync(ClaimsPrincipal user, AdminDbContext db)
    {
        var employeeId = UserId(user);
        var claimRoles = user.Claims.Where(x => x.Type is "role" or "roles" or ClaimTypes.Role).Select(x => x.Value).ToList();
        return await db.EmployeeRoles.Where(x => x.IsActive && x.EmployeeId == employeeId).Join(db.Roles.Where(x => x.IsActive), assignment => assignment.RoleId, role => role.RoleId, (_, role) => role.Code).Where(code => claimRoles.Contains(code)).SingleOrDefaultAsync();
    }
    private static string UserId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("employee_id") ?? user.FindFirstValue("sub") ?? "authenticated-user";

    private static async Task<IResult> CreateMaster(string name, DbSet<AdminApplicationType> set, AdminDbContext db)
    {
        if (string.IsNullOrWhiteSpace(name) || await set.AnyAsync(x => x.ApplicationTypeName == name.Trim())) return Results.BadRequest(new { message = "A master record with this name already exists or the name is empty." });
        set.Add(new AdminApplicationType { ApplicationTypeName = name.Trim(), IsActive = true }); await db.SaveChangesAsync(); return Results.Ok();
    }
    private static async Task<IResult> UpdateApplicationType(int id, MasterRequest request, AdminDbContext db) { var item = await db.ApplicationTypes.FindAsync(id); if (item is null) return Results.NotFound(); item.ApplicationTypeName = request.Name.Trim(); item.IsActive = request.IsActive; await db.SaveChangesAsync(); return Results.Ok(); }
    private static async Task<IResult> DeactivateApplicationType(int id, AdminDbContext db) { var item = await db.ApplicationTypes.FindAsync(id); if (item is null) return Results.NotFound(); item.IsActive = false; await db.SaveChangesAsync(); return Results.NoContent(); }
    private static async Task<IResult> CreateApplicantType(MasterRequest request, AdminDbContext db) { if (string.IsNullOrWhiteSpace(request.Name) || await db.ApplicantTypes.AnyAsync(x => x.ApplicantTypeName == request.Name.Trim())) return Results.BadRequest(new { message = "A master record with this name already exists or the name is empty." }); db.ApplicantTypes.Add(new AdminApplicantType { ApplicantTypeName = request.Name.Trim(), IsActive = true }); await db.SaveChangesAsync(); return Results.Ok(); }
    private static async Task<IResult> UpdateApplicantType(int id, MasterRequest request, AdminDbContext db) { var item = await db.ApplicantTypes.FindAsync(id); if (item is null) return Results.NotFound(); item.ApplicantTypeName = request.Name.Trim(); item.IsActive = request.IsActive; await db.SaveChangesAsync(); return Results.Ok(); }
    private static async Task<IResult> DeactivateApplicantType(int id, AdminDbContext db) { var item = await db.ApplicantTypes.FindAsync(id); if (item is null) return Results.NotFound(); item.IsActive = false; await db.SaveChangesAsync(); return Results.NoContent(); }
    private static async Task<IResult> CreateDocumentType(MasterRequest request, AdminDbContext db) { if (string.IsNullOrWhiteSpace(request.Name) || await db.DocumentTypes.AnyAsync(x => x.DocumentTypeName == request.Name.Trim())) return Results.BadRequest(new { message = "A master record with this name already exists or the name is empty." }); db.DocumentTypes.Add(new AdminDocumentType { DocumentTypeName = request.Name.Trim(), IsActive = true }); await db.SaveChangesAsync(); return Results.Ok(); }
    private static async Task<IResult> UpdateDocumentType(int id, MasterRequest request, AdminDbContext db) { var item = await db.DocumentTypes.FindAsync(id); if (item is null) return Results.NotFound(); item.DocumentTypeName = request.Name.Trim(); item.IsActive = request.IsActive; await db.SaveChangesAsync(); return Results.Ok(); }
    private static async Task<IResult> DeactivateDocumentType(int id, AdminDbContext db) { var item = await db.DocumentTypes.FindAsync(id); if (item is null) return Results.NotFound(); item.IsActive = false; await db.SaveChangesAsync(); return Results.NoContent(); }

    private static async Task<IResult> GetDocumentFile(int applicationId, int documentId, AdminDbContext db, IWebHostEnvironment environment, IConfiguration config)
    {
        var document = await db.ApplicationDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.ApplicationDocumentId == documentId);
        return document is null ? Results.NotFound() : StoredFile(document.FilePath, document.ContentType, document.FileName, environment, config);
    }
    private static async Task<IResult> GetPhotoFile(int applicationId, int photoId, AdminDbContext db, IWebHostEnvironment environment, IConfiguration config)
    {
        var photo = await db.ApplicationPhotos.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.ApplicationPhotoId == photoId);
        return photo is null ? Results.NotFound() : StoredFile(photo.FilePath, photo.ContentType, photo.FileName, environment, config);
    }
    private static IResult StoredFile(string relativePath, string contentType, string fileName, IWebHostEnvironment environment, IConfiguration config)
    {
        var root = config["CitizenStorageRoot"] ?? Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "User-End", "Backend"));
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return fullPath.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath) ? Results.File(fullPath, contentType, fileName, enableRangeProcessing: true) : Results.NotFound();
    }
}

public abstract record ReviewRequestBase(string Remarks, string Recommendation);
public record ReviewRequest(string Remarks, string Recommendation) : ReviewRequestBase(Remarks, Recommendation);
public record ConstructionReviewRequest(string Remarks, string Recommendation) : ReviewRequestBase(Remarks, Recommendation);
public record InspectionRequest(DateTime? InspectionDate, string InspectionFindings, string Remarks, string Recommendation, bool ConstructionRelated) : ReviewRequestBase(Remarks, Recommendation);
public record DecisionRequest(string Remarks, string Recommendation, string Decision) : ReviewRequestBase(Remarks, Recommendation);
public record MasterRequest(string Name, bool IsActive = true);