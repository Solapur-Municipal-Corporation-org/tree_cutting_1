using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TreeCutting.Api.Models;

public class ApplicationType
{
    [Key]
    public int ApplicationTypeId { get; set; }

    [Required, MaxLength(200)]
    public string ApplicationTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationTypeDocumentMapping> RequiredDocuments { get; set; } = new List<ApplicationTypeDocumentMapping>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

public class ApplicantType
{
    [Key]
    public int ApplicantTypeId { get; set; }

    [Required, MaxLength(100)]
    public string ApplicantTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Application> Applications { get; set; } = new List<Application>();
}

public class DocumentType
{
    [Key]
    public int DocumentTypeId { get; set; }

    [Required, MaxLength(200)]
    public string DocumentTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<ApplicationTypeDocumentMapping> ApplicationTypeMappings { get; set; } = new List<ApplicationTypeDocumentMapping>();
    public ICollection<ApplicationDocument> ApplicationDocuments { get; set; } = new List<ApplicationDocument>();
}

public class ApplicationTypeDocumentMapping
{
    [Key]
    public int ApplicationTypeDocumentMappingId { get; set; }

    [ForeignKey(nameof(ApplicationType))]
    public int ApplicationTypeId { get; set; }

    [ForeignKey(nameof(DocumentType))]
    public int DocumentTypeId { get; set; }

    public bool IsRequired { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ApplicationType ApplicationType { get; set; } = null!;
    public DocumentType DocumentType { get; set; } = null!;
}

public class Application
{
    [Key]
    public int ApplicationId { get; set; }

    [Required, MaxLength(50)]
    public string ApplicationNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CitizenId { get; set; }

    [ForeignKey(nameof(ApplicationType))]
    public int ApplicationTypeId { get; set; }

    [ForeignKey(nameof(ApplicantType))]
    public int ApplicantTypeId { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200), RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
    public string EmailId { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20), RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Mobile number must be 10 digits and start with 6-9.")]
    public string MobileNo { get; set; } = string.Empty;

    [Required, MaxLength(12), MinLength(12), RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhar number must be exactly 12 digits.")]
    public string AadharNo { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PetName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string PethNo { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ZoneNo { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string PrabhagNo { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string PropertyTaxNo { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string TreeAddress { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string TreeCuttingReason { get; set; } = string.Empty;

    [Required, Range(1, int.MaxValue, ErrorMessage = "Number of trees must be at least 1.")]
    public int NumberOfTreeCutting { get; set; }

    [Required, MaxLength(200)]
    public string TreeSpecies { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public bool IsSubmitted { get; set; }
    public string Status { get; set; } = "Draft";
    public bool SmsSent { get; set; }
    public DateTime? SmsSentDate { get; set; }
    [MaxLength(500)]
    public string? SmsError { get; set; }

    public ApplicationType ApplicationType { get; set; } = null!;
    public ApplicantType ApplicantType { get; set; } = null!;
    public ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
    public ICollection<ApplicationPhoto> Photos { get; set; } = new List<ApplicationPhoto>();
}

public class ApplicationDocument
{
    [Key]
    public int ApplicationDocumentId { get; set; }

    [ForeignKey(nameof(Application))]
    public int ApplicationId { get; set; }

    [ForeignKey(nameof(ApplicationType))]
    public int ApplicationTypeId { get; set; }

    [ForeignKey(nameof(DocumentType))]
    public int DocumentTypeId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public Application Application { get; set; } = null!;
    public ApplicationType ApplicationType { get; set; } = null!;
    public DocumentType DocumentType { get; set; } = null!;
}

public class ApplicationPhoto
{
    [Key]
    public int ApplicationPhotoId { get; set; }

    [ForeignKey(nameof(Application))]
    public int ApplicationId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContentType { get; set; } = string.Empty;

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public Application Application { get; set; } = null!;
}
