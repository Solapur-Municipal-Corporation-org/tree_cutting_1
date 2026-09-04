using System.ComponentModel.DataAnnotations;

namespace AdminPanel.Api.Models;

public sealed class AdminApplication
{
    [Key] public int ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = "";
    public string? CitizenId { get; set; }
    public int ApplicationTypeId { get; set; }
    public int ApplicantTypeId { get; set; }
    public string FullName { get; set; } = "";
    public string Address { get; set; } = "";
    public string EmailId { get; set; } = "";
    public string MobileNo { get; set; } = "";
    public string AadharNo { get; set; } = "";
    public string PetName { get; set; } = "";
    public string PethNo { get; set; } = "";
    public string ZoneNo { get; set; } = "";
    public string PrabhagNo { get; set; } = "";
    public string PropertyTaxNo { get; set; } = "";
    public string TreeAddress { get; set; } = "";
    public string TreeCuttingReason { get; set; } = "";
    public int NumberOfTreeCutting { get; set; }
    public string TreeSpecies { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public bool IsSubmitted { get; set; }
    public string Status { get; set; } = "Draft";
    public AdminApplicationType ApplicationType { get; set; } = null!;
    public AdminApplicantType ApplicantType { get; set; } = null!;
}

public sealed class AdminApplicationType { [Key] public int ApplicationTypeId { get; set; } public string ApplicationTypeName { get; set; } = ""; public bool IsActive { get; set; } }
public sealed class AdminApplicantType { [Key] public int ApplicantTypeId { get; set; } public string ApplicantTypeName { get; set; } = ""; public bool IsActive { get; set; } }
public sealed class AdminDocumentType { [Key] public int DocumentTypeId { get; set; } public string DocumentTypeName { get; set; } = ""; public bool IsActive { get; set; } }

public sealed class AdminApplicationDocument
{
    [Key] public int ApplicationDocumentId { get; set; }
    public int ApplicationId { get; set; }
    public int DocumentTypeId { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public DateTime UploadedDate { get; set; }
    public AdminApplication Application { get; set; } = null!;
    public AdminDocumentType DocumentType { get; set; } = null!;
}

public sealed class AdminApplicationPhoto
{
    [Key] public int ApplicationPhotoId { get; set; }
    public int ApplicationId { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public DateTime UploadedDate { get; set; }
    public AdminApplication Application { get; set; } = null!;
}

public sealed class WorkflowHistory
{
    [Key] public long WorkflowId { get; set; }
    public int ApplicationId { get; set; }
    public string FromRole { get; set; } = "";
    public string ToRole { get; set; } = "";
    public string Action { get; set; } = "";
    public string PreviousStatus { get; set; } = "";
    public string NewStatus { get; set; } = "";
    public string? Remarks { get; set; }
    public string ActionBy { get; set; } = "";
    public DateTime ActionDate { get; set; }
}

public sealed class DepartmentReview
{
    [Key] public long DepartmentReviewId { get; set; }
    public int ApplicationId { get; set; }
    public string Role { get; set; } = "";
    public DateTime ReviewDate { get; set; }
    public DateTime? InspectionDate { get; set; }
    public string? InspectionFindings { get; set; }
    public string Remarks { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public bool? ConstructionRelated { get; set; }
    public string? Decision { get; set; }
    public string ActionBy { get; set; } = "";
}

public sealed class AdminDepartment { [Key] public int DepartmentId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public bool IsActive { get; set; } }
public sealed class AdminDesignation { [Key] public int DesignationId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public int? DepartmentId { get; set; } public bool IsActive { get; set; } }
public sealed class AdminRole { [Key] public int RoleId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public int? DepartmentId { get; set; } public bool IsActive { get; set; } }
public sealed class AdminEmployeeRole { [Key] public long EmployeeRoleId { get; set; } public string EmployeeId { get; set; } = ""; public int RoleId { get; set; } public int? DesignationId { get; set; } public bool IsActive { get; set; } }
public sealed class WorkflowStatus { [Key] public int WorkflowStatusId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public bool IsTerminal { get; set; } public bool IsActive { get; set; } }
public sealed class WorkflowStage { [Key] public int WorkflowStageId { get; set; } public string Code { get; set; } = ""; public int RoleId { get; set; } public int PendingStatusId { get; set; } public bool IsActive { get; set; } }
public sealed class WorkflowTransition { [Key] public long WorkflowTransitionId { get; set; } public int FromStatusId { get; set; } public int FromRoleId { get; set; } public string ActionCode { get; set; } = ""; public int ToStatusId { get; set; } public string? ConditionField { get; set; } public string? ConditionValue { get; set; } public bool IsActive { get; set; } }
public sealed class WorkflowDecision { [Key] public int WorkflowDecisionId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public bool IsActive { get; set; } }
public sealed class AdminZone { [Key] public int ZoneId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public bool IsActive { get; set; } }
public sealed class AdminPrabhag { [Key] public int PrabhagId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public int? ZoneId { get; set; } public bool IsActive { get; set; } }
public sealed class AdminWard { [Key] public int WardId { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public int? PrabhagId { get; set; } public bool IsActive { get; set; } }