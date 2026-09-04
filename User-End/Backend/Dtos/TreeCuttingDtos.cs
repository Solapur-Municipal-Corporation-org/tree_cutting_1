namespace TreeCutting.Api.Dtos;

public record MasterOptionDto(int Id, string Name);

public record DocumentRequirementDto(
    int DocumentTypeId,
    string DocumentTypeName,
    bool IsRequired,
    int DisplayOrder);

public record ApplicationCreateRequest(
    int ApplicationTypeId,
    int ApplicantTypeId,
    string FullName,
    string Address,
    string EmailId,
    string MobileNo,
    string AadharNo,
    string PetName,
    string PethNo,
    string ZoneNo,
    string PrabhagNo,
    string PropertyTaxNo,
    string TreeAddress,
    string TreeCuttingReason,
    int NumberOfTreeCutting,
    string TreeSpecies);

public record ApplicationUpdateRequest(
    int ApplicationTypeId,
    int ApplicantTypeId,
    string FullName,
    string Address,
    string EmailId,
    string MobileNo,
    string AadharNo,
    string PetName,
    string PethNo,
    string ZoneNo,
    string PrabhagNo,
    string PropertyTaxNo,
    string TreeAddress,
    string TreeCuttingReason,
    int NumberOfTreeCutting,
    string TreeSpecies);

public record ApplicationDocumentDto(
    int ApplicationDocumentId,
    int ApplicationId,
    int ApplicationTypeId,
    int DocumentTypeId,
    string DocumentTypeName,
    string FileName,
    string FilePath,
    string ContentType,
    DateTime UploadedDate);

public record ApplicationPhotoDto(
    int ApplicationPhotoId,
    int ApplicationId,
    string FileName,
    string FilePath,
    string ContentType,
    DateTime UploadedDate);

public record ApplicationDetailDto(
    int ApplicationId,
    string ApplicationNumber,
    int ApplicationTypeId,
    string ApplicationTypeName,
    int ApplicantTypeId,
    string ApplicantTypeName,
    string FullName,
    string Address,
    string EmailId,
    string MobileNo,
    string AadharNo,
    string PetName,
    string PethNo,
    string ZoneNo,
    string PrabhagNo,
    string PropertyTaxNo,
    string TreeAddress,
    string TreeCuttingReason,
    int NumberOfTreeCutting,
    string TreeSpecies,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    DateTime? SubmittedDate,
    bool IsSubmitted,
    string Status,
    List<ApplicationDocumentDto> Documents,
    List<ApplicationPhotoDto> Photos);

public record ApplicationCreateResponse(
    int ApplicationId,
    string ApplicationNumber);

public record CitizenProfileDto(
    string CitizenId,
    string Name,
    string MobileNumber,
    string Email,
    string Address);

public record CitizenApplicationSummaryDto(
    int ApplicationId,
    string ApplicationNumber,
    string ApplicationTypeName,
    string Status,
    DateTime CreatedDate,
    DateTime? UpdatedDate,
    DateTime? SubmittedDate);

public record UploadDocumentRequest(
    int ApplicationTypeId,
    int DocumentTypeId);

