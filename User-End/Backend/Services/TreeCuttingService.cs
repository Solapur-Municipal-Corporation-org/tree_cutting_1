using System.Data;
using Microsoft.EntityFrameworkCore;
using TreeCutting.Api.Data;
using TreeCutting.Api.Dtos;
using TreeCutting.Api.Models;

namespace TreeCutting.Api.Services;

public class TreeCuttingService
{
    private readonly TreeCuttingDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ISmsGateway _smsGateway;
    private readonly SmsGatewayOptions _smsGatewayOptions;
    private readonly ILogger<TreeCuttingService> _logger;

    public TreeCuttingService(
        TreeCuttingDbContext context,
        IWebHostEnvironment environment,
        ISmsGateway smsGateway,
        Microsoft.Extensions.Options.IOptions<SmsGatewayOptions> smsGatewayOptions,
        ILogger<TreeCuttingService> logger)
    {
        _context = context;
        _environment = environment;
        _smsGateway = smsGateway;
        _smsGatewayOptions = smsGatewayOptions.Value;
        _logger = logger;
    }

    public async Task<List<MasterOptionDto>> GetApplicationTypesAsync()
    {
        return await _context.ApplicationTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.ApplicationTypeId)
            .Select(x => new MasterOptionDto(x.ApplicationTypeId, x.ApplicationTypeName))
            .ToListAsync();
    }

    public async Task<List<MasterOptionDto>> GetApplicantTypesAsync()
    {
        return await _context.ApplicantTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.ApplicantTypeId)
            .Select(x => new MasterOptionDto(x.ApplicantTypeId, x.ApplicantTypeName))
            .ToListAsync();
    }

    public async Task<List<MasterOptionDto>> GetZonesAsync()
    {
        var zoneNames = await _context.Applications
            .AsNoTracking()
            .Where(x => x.ZoneNo != null && x.ZoneNo != string.Empty)
            .Select(x => x.ZoneNo)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        return zoneNames.Select((name, index) => new MasterOptionDto(index + 1, name)).ToList();
    }

    public async Task<List<MasterOptionDto>> GetPethsAsync()
    {
        var pethNames = await _context.Applications
            .AsNoTracking()
            .Where(x => x.PetName != null && x.PetName != string.Empty)
            .Select(x => x.PetName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        return pethNames.Select((name, index) => new MasterOptionDto(index + 1, name)).ToList();
    }

    public async Task<List<MasterOptionDto>> GetPrabhaksAsync()
    {
        var prabhagNames = await _context.Applications
            .AsNoTracking()
            .Where(x => x.PrabhagNo != null && x.PrabhagNo != string.Empty)
            .Select(x => x.PrabhagNo)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        return prabhagNames.Select((name, index) => new MasterOptionDto(index + 1, name)).ToList();
    }

    public async Task<List<DocumentRequirementDto>> GetDocumentsByApplicationTypeAsync(int applicationTypeId)
    {
        return await _context.ApplicationTypeDocumentMappings
            .Include(x => x.DocumentType)
            .Where(x => x.ApplicationTypeId == applicationTypeId && x.IsRequired)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new DocumentRequirementDto(
                x.DocumentType.DocumentTypeId,
                x.DocumentType.DocumentTypeName,
                x.IsRequired,
                x.DisplayOrder))
            .ToListAsync();
    }

    private async Task<string> GenerateApplicationNumberAsync()
    {
        var currentYear = DateTime.UtcNow.Year;
        var lastApplication = await _context.Applications
            .AsNoTracking()
            .OrderByDescending(x => x.ApplicationId)
            .FirstOrDefaultAsync();

        int sequenceNumber = 1;
        if (lastApplication != null && lastApplication.ApplicationNumber.Contains(currentYear.ToString()))
        {
            var lastSequence = lastApplication.ApplicationNumber.Split('-').Last();
            if (int.TryParse(lastSequence, out int parsed))
            {
                sequenceNumber = parsed + 1;
            }
        }

        return $"SMC_Tree-{currentYear}-{sequenceNumber:D3}";
    }

    public async Task<ApplicationDetailDto> CreateApplicationAsync(ApplicationCreateRequest request, string? citizenId = null)
    {
        ValidateApplicationRequest(request);

        var applicationTypeExists = await _context.ApplicationTypes.AnyAsync(x => x.ApplicationTypeId == request.ApplicationTypeId && x.IsActive);
        if (!applicationTypeExists)
        {
            throw new InvalidOperationException("Selected application type is invalid.");
        }

        var applicantTypeExists = await _context.ApplicantTypes.AnyAsync(x => x.ApplicantTypeId == request.ApplicantTypeId && x.IsActive);
        if (!applicantTypeExists)
        {
            throw new InvalidOperationException("Selected applicant type is invalid.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var applicationNumber = await GenerateApplicationNumberAsync();

        var application = new Application
        {
            ApplicationNumber = applicationNumber,
            CitizenId = string.IsNullOrWhiteSpace(citizenId) ? null : citizenId.Trim(),
            ApplicationTypeId = request.ApplicationTypeId,
            ApplicantTypeId = request.ApplicantTypeId,
            FullName = request.FullName.Trim(),
            Address = request.Address.Trim(),
            EmailId = request.EmailId.Trim(),
            MobileNo = request.MobileNo.Trim(),
            AadharNo = request.AadharNo.Trim(),
            PetName = request.PetName.Trim(),
            PethNo = request.PethNo.Trim(),
            ZoneNo = request.ZoneNo.Trim(),
            PrabhagNo = request.PrabhagNo.Trim(),
            PropertyTaxNo = request.PropertyTaxNo.Trim(),
            TreeAddress = request.TreeAddress.Trim(),
            TreeCuttingReason = request.TreeCuttingReason.Trim(),
            NumberOfTreeCutting = request.NumberOfTreeCutting,
            TreeSpecies = request.TreeSpecies.Trim(),
            Status = "Draft",
            IsSubmitted = false,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = null,
            SubmittedDate = null
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetApplicationByIdAsync(application.ApplicationId) ?? throw new InvalidOperationException("Unable to load the created application.");
    }

    public async Task<List<CitizenApplicationSummaryDto>> GetCitizenApplicationsAsync(string citizenId)
    {
        return await _context.Applications
            .AsNoTracking()
            .Where(x => x.CitizenId == citizenId)
            .Join(_context.ApplicationTypes, application => application.ApplicationTypeId, type => type.ApplicationTypeId,
                (application, type) => new CitizenApplicationSummaryDto(
                    application.ApplicationId,
                    application.ApplicationNumber,
                    type.ApplicationTypeName,
                    application.Status,
                    application.CreatedDate,
                    application.UpdatedDate,
                    application.SubmittedDate))
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();
    }

    public async Task<ApplicationDetailDto?> GetCitizenApplicationByIdAsync(int applicationId, string citizenId)
    {
        var belongsToCitizen = await _context.Applications
            .AsNoTracking()
            .AnyAsync(x => x.ApplicationId == applicationId && x.CitizenId == citizenId);

        return belongsToCitizen ? await GetApplicationByIdAsync(applicationId) : null;
    }

    public async Task<bool> CitizenApplicationBelongsToAsync(int applicationId, string citizenId)
    {
        return await _context.Applications
            .AsNoTracking()
            .AnyAsync(x => x.ApplicationId == applicationId && x.CitizenId == citizenId);
    }

    public async Task<ApplicationPhotoDto> UploadPhotoAsync(int applicationId, string citizenId, int? photoId, IFormFile file)
    {
        var application = await _context.Applications
            .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.CitizenId == citizenId);

        if (application is null)
        {
            throw new InvalidOperationException("Application not found.");
        }

        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException("Photo size should be greater than 0 and 5 MB or less.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png")
        {
            throw new InvalidOperationException("Only JPG, JPEG, and PNG photos are allowed.");
        }

        var existingPhoto = photoId.HasValue
            ? await _context.ApplicationPhotos.SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.ApplicationPhotoId == photoId.Value)
            : null;

        if (photoId.HasValue && existingPhoto is null)
        {
            throw new InvalidOperationException("Photo not found.");
        }

        var directoryPath = Path.Combine(_environment.ContentRootPath, "Uploads", applicationId.ToString(), "photos");
        Directory.CreateDirectory(directoryPath);
        var safeFileName = Path.GetFileName(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid():N}_{safeFileName}";
        var finalPath = Path.Combine(directoryPath, uniqueFileName);

        await using (var stream = new FileStream(finalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeFilePath = Path.Combine("Uploads", applicationId.ToString(), "photos", uniqueFileName).Replace('\\', '/');
        if (existingPhoto is not null)
        {
            DeleteStoredFile(existingPhoto.FilePath);
            existingPhoto.FileName = safeFileName;
            existingPhoto.FilePath = relativeFilePath;
            existingPhoto.ContentType = file.ContentType;
            existingPhoto.UploadedDate = DateTime.UtcNow;
        }
        else
        {
            existingPhoto = new ApplicationPhoto
            {
                ApplicationId = applicationId,
                FileName = safeFileName,
                FilePath = relativeFilePath,
                ContentType = file.ContentType,
                UploadedDate = DateTime.UtcNow
            };
            _context.ApplicationPhotos.Add(existingPhoto);
        }

        await _context.SaveChangesAsync();
        return new ApplicationPhotoDto(existingPhoto.ApplicationPhotoId, existingPhoto.ApplicationId, existingPhoto.FileName, existingPhoto.FilePath, existingPhoto.ContentType, existingPhoto.UploadedDate);
    }

    public async Task<bool> DeletePhotoAsync(int applicationId, string citizenId, int photoId)
    {
        var photo = await _context.ApplicationPhotos
            .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.Application.CitizenId == citizenId && x.ApplicationPhotoId == photoId);

        if (photo is null)
        {
            return false;
        }

        DeleteStoredFile(photo.FilePath);
        _context.ApplicationPhotos.Remove(photo);
        await _context.SaveChangesAsync();
        return true;
    }

    private void DeleteStoredFile(string relativePath)
    {
        var physicalPath = Path.Combine(_environment.ContentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
    }

    public async Task<ApplicationDetailDto?> GetApplicationByIdAsync(int applicationId)
    {
        var application = await _context.Applications
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApplicationId == applicationId);

        if (application == null)
        {
            return null;
        }

        var applicationTypeName = await _context.ApplicationTypes
            .AsNoTracking()
            .Where(x => x.ApplicationTypeId == application.ApplicationTypeId)
            .Select(x => x.ApplicationTypeName)
            .SingleOrDefaultAsync() ?? string.Empty;

        var applicantTypeName = await _context.ApplicantTypes
            .AsNoTracking()
            .Where(x => x.ApplicantTypeId == application.ApplicantTypeId)
            .Select(x => x.ApplicantTypeName)
            .SingleOrDefaultAsync() ?? string.Empty;

        var documents = await _context.ApplicationDocuments
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .OrderBy(x => x.UploadedDate)
            .ToListAsync();

        var photos = await _context.ApplicationPhotos
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .OrderBy(x => x.UploadedDate)
            .ToListAsync();

        var documentTypeNames = await _context.DocumentTypes
            .AsNoTracking()
            .Where(x => documents.Select(d => d.DocumentTypeId).Contains(x.DocumentTypeId))
            .ToDictionaryAsync(x => x.DocumentTypeId, x => x.DocumentTypeName);

        return MapToDetailDto(application, applicationTypeName, applicantTypeName, documents, photos, documentTypeNames);
    }

    public async Task<ApplicationDetailDto?> UpdateApplicationAsync(int applicationId, ApplicationUpdateRequest request)
    {
        var application = await _context.Applications
            .SingleOrDefaultAsync(x => x.ApplicationId == applicationId);

        if (application == null)
        {
            return null;
        }

        application.ApplicationTypeId = request.ApplicationTypeId;
        application.ApplicantTypeId = request.ApplicantTypeId;
        application.FullName = request.FullName.Trim();
        application.Address = request.Address.Trim();
        application.EmailId = request.EmailId.Trim();
        application.MobileNo = request.MobileNo.Trim();
        application.AadharNo = request.AadharNo.Trim();
        application.PetName = request.PetName.Trim();
        application.PethNo = request.PethNo.Trim();
        application.ZoneNo = request.ZoneNo.Trim();
        application.PrabhagNo = request.PrabhagNo.Trim();
        application.PropertyTaxNo = request.PropertyTaxNo.Trim();
        application.TreeAddress = request.TreeAddress.Trim();
        application.TreeCuttingReason = request.TreeCuttingReason.Trim();
        application.NumberOfTreeCutting = request.NumberOfTreeCutting;
        application.TreeSpecies = request.TreeSpecies.Trim();
        application.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetApplicationByIdAsync(applicationId);
    }

    public async Task<ApplicationDetailDto?> SubmitApplicationAsync(int applicationId)
    {
        var application = await _context.Applications
            .SingleOrDefaultAsync(x => x.ApplicationId == applicationId);

        if (application == null)
        {
            return null;
        }

        var requiredDocuments = await GetDocumentsByApplicationTypeAsync(application.ApplicationTypeId);
        if (requiredDocuments.Count > 0)
        {
            var requiredDocumentIds = requiredDocuments.Select(x => x.DocumentTypeId).ToHashSet();
            var uploadedDocumentIds = await _context.ApplicationDocuments
                .AsNoTracking()
                .Where(x => x.ApplicationId == applicationId && !string.IsNullOrWhiteSpace(x.FilePath))
                .Select(x => x.DocumentTypeId)
                .ToListAsync();

            var missing = requiredDocumentIds.Except(uploadedDocumentIds).Any();
            if (missing)
            {
                throw new InvalidOperationException("All required documents must be uploaded before submitting the application.");
            }
        }

        application.IsSubmitted = true;
        application.Status = "Submitted";
        application.SubmittedDate ??= DateTime.UtcNow;
        application.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        if (!application.SmsSent)
        {
            try
            {
                var smsSent = await _smsGateway.SendSmsAsync(
                    BuildSubmissionSms(application),
                    application.MobileNo);

                if (smsSent)
                {
                    application.SmsSent = true;
                    application.SmsSentDate = DateTime.UtcNow;
                    application.SmsError = null;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                application.SmsError = ex.Message.Length <= 500 ? ex.Message : ex.Message[..500];
                await _context.SaveChangesAsync();
                _logger.LogWarning(ex, "Unable to send submission SMS for application {ApplicationNumber}.", application.ApplicationNumber);
            }
        }

        return await GetApplicationByIdAsync(applicationId);
    }

    private string BuildSubmissionSms(Application application)
    {
        return _smsGatewayOptions.MessageTemplate
            .Replace("{FullName}", application.FullName, StringComparison.Ordinal)
            .Replace("{ApplicationNumber}", application.ApplicationNumber, StringComparison.Ordinal);
    }

    public async Task<List<ApplicationDocumentDto>> GetApplicationDocumentsAsync(int applicationId)
    {
        return await _context.ApplicationDocuments
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .OrderBy(x => x.UploadedDate)
            .Select(x => new ApplicationDocumentDto(
                x.ApplicationDocumentId,
                x.ApplicationId,
                x.ApplicationTypeId,
                x.DocumentTypeId,
                string.Empty,
                x.FileName,
                x.FilePath,
                x.ContentType,
                x.UploadedDate))
            .ToListAsync();
    }

    public async Task<ApplicationDocumentDto> UploadDocumentAsync(int applicationId, int applicationTypeId, int documentTypeId, IFormFile file)
    {
        var application = await _context.Applications
            .SingleOrDefaultAsync(x => x.ApplicationId == applicationId);

        if (application == null)
        {
            throw new InvalidOperationException("Application not found.");
        }

        var validDocumentType = await _context.ApplicationTypeDocumentMappings
            .AnyAsync(x => x.ApplicationTypeId == applicationTypeId && x.DocumentTypeId == documentTypeId && x.IsRequired);

        if (!validDocumentType)
        {
            throw new InvalidOperationException("This document is not required for the selected application type.");
        }

        if (file.Length <= 0)
        {
            throw new InvalidOperationException("A valid file is required to upload.");
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            throw new InvalidOperationException("Document size should be 5 MB or less.");
        }

        var documentTypeName = await _context.DocumentTypes
            .Where(x => x.DocumentTypeId == documentTypeId)
            .Select(x => x.DocumentTypeName)
            .SingleOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(documentTypeName))
        {
            throw new InvalidOperationException("Selected document type is invalid.");
        }

        var photoDocumentNames = new[]
        {
            "Photo of tree",
            "Photo of the tree",
            "Photo of the tree from all angles"
        };

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension.ToLowerInvariant()))
        {
            throw new InvalidOperationException("Only PDF, JPG, JPEG, and PNG files are allowed.");
        }

        var isPhotoDocument = photoDocumentNames.Contains(documentTypeName, StringComparer.OrdinalIgnoreCase);
        var requiredExtensionSet = isPhotoDocument ? new[] { ".jpg", ".jpeg", ".png" } : new[] { ".pdf" };

        if (!requiredExtensionSet.Contains(extension.ToLowerInvariant()))
        {
            var expectedType = isPhotoDocument ? "JPEG, JPG, or PNG" : "PDF";
            throw new InvalidOperationException($"{documentTypeName} must be uploaded as {expectedType}.");
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            var contentType = file.ContentType.Trim().ToLowerInvariant();
            var allowedMimeTypes = isPhotoDocument
                ? new[] { "image/jpeg", "image/png" }
                : new[] { "application/pdf" };

            var isGenericMimeType = contentType is "application/octet-stream" or "binary/octet-stream" or "application/x-download";
            if (!isGenericMimeType && !allowedMimeTypes.Contains(contentType))
            {
                var expectedTypeLabel = isPhotoDocument ? "JPEG, JPG, or PNG" : "PDF";
                throw new InvalidOperationException($"{documentTypeName} must be uploaded as {expectedTypeLabel}.");
            }
        }

        var safeFileName = Path.GetFileName(file.FileName);
        var directoryPath = Path.Combine(_environment.ContentRootPath, "Uploads", applicationId.ToString(), applicationTypeId.ToString());
        Directory.CreateDirectory(directoryPath);

        var uniqueFileName = $"{Guid.NewGuid():N}_{safeFileName}";
        var finalPath = Path.Combine(directoryPath, uniqueFileName);

        await using (var stream = new FileStream(finalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeFilePath = Path.Combine("Uploads", applicationId.ToString(), applicationTypeId.ToString(), uniqueFileName)
            .Replace('\\', '/');

        var documentType = await _context.DocumentTypes.SingleAsync(x => x.DocumentTypeId == documentTypeId);
        var existingDocument = await _context.ApplicationDocuments
            .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.DocumentTypeId == documentTypeId);

        if (existingDocument != null)
        {
            var existingPhysicalPath = Path.Combine(_environment.ContentRootPath, existingDocument.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(existingPhysicalPath))
            {
                File.Delete(existingPhysicalPath);
            }

            existingDocument.FileName = safeFileName;
            existingDocument.FilePath = relativeFilePath;
            existingDocument.ContentType = file.ContentType;
            existingDocument.UploadedDate = DateTime.UtcNow;
            existingDocument.ApplicationTypeId = applicationTypeId;

            await _context.SaveChangesAsync();

            return MapToDocumentDto(existingDocument, documentType.DocumentTypeName);
        }

        var entity = new ApplicationDocument
        {
            ApplicationId = applicationId,
            ApplicationTypeId = applicationTypeId,
            DocumentTypeId = documentTypeId,
            FileName = safeFileName,
            FilePath = relativeFilePath,
            ContentType = file.ContentType,
            UploadedDate = DateTime.UtcNow
        };

        _context.ApplicationDocuments.Add(entity);
        await _context.SaveChangesAsync();

        return MapToDocumentDto(entity, documentTypeName);
    }

    public async Task<bool> DeleteDocumentAsync(int applicationId, int documentId)
    {
        var document = await _context.ApplicationDocuments
            .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.ApplicationDocumentId == documentId);

        if (document == null)
        {
            return false;
        }

        var physicalPath = Path.Combine(_environment.ContentRootPath, document.FilePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        _context.ApplicationDocuments.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    private static void ValidateApplicationRequest(ApplicationCreateRequest request)
    {
        if (request.NumberOfTreeCutting <= 0)
        {
            throw new InvalidOperationException("Number of trees must be at least 1.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.MobileNo, @"^[6-9]\d{9}$"))
        {
            throw new InvalidOperationException("Mobile number must be 10 digits and start with 6-9.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.AadharNo, @"^\d{12}$"))
        {
            throw new InvalidOperationException("Aadhar number must be exactly 12 digits.");
        }

        if (string.IsNullOrWhiteSpace(request.EmailId) || !request.EmailId.Contains("@"))
        {
            throw new InvalidOperationException("Invalid email address.");
        }
    }

    private static ApplicationDetailDto MapToDetailDto(
        Application application,
        string applicationTypeName,
        string applicantTypeName,
        IEnumerable<ApplicationDocument> documents,
        IEnumerable<ApplicationPhoto> photos,
        IReadOnlyDictionary<int, string> documentTypeNames)
    {
        return new ApplicationDetailDto(
            application.ApplicationId,
            application.ApplicationNumber,
            application.ApplicationTypeId,
            applicationTypeName,
            application.ApplicantTypeId,
            applicantTypeName,
            application.FullName,
            application.Address,
            application.EmailId,
            application.MobileNo,
            application.AadharNo,
            application.PetName,
            application.PethNo,
            application.ZoneNo,
            application.PrabhagNo,
            application.PropertyTaxNo,
            application.TreeAddress,
            application.TreeCuttingReason,
            application.NumberOfTreeCutting,
            application.TreeSpecies,
            application.CreatedDate,
            application.UpdatedDate,
            application.SubmittedDate,
            application.IsSubmitted,
            application.Status,
            documents
                .OrderBy(x => x.UploadedDate)
                .Select(x => new ApplicationDocumentDto(
                    x.ApplicationDocumentId,
                    x.ApplicationId,
                    x.ApplicationTypeId,
                    x.DocumentTypeId,
                    documentTypeNames.TryGetValue(x.DocumentTypeId, out var documentTypeName) ? documentTypeName : string.Empty,
                    x.FileName,
                    x.FilePath,
                    x.ContentType,
                    x.UploadedDate))
                .ToList(),
            photos
                .OrderBy(x => x.UploadedDate)
                .Select(x => new ApplicationPhotoDto(
                    x.ApplicationPhotoId,
                    x.ApplicationId,
                    x.FileName,
                    x.FilePath,
                    x.ContentType,
                    x.UploadedDate))
                .ToList());
    }

    private static ApplicationDocumentDto MapToDocumentDto(ApplicationDocument document, string documentTypeName)
    {
        return new ApplicationDocumentDto(
            document.ApplicationDocumentId,
            document.ApplicationId,
            document.ApplicationTypeId,
            document.DocumentTypeId,
            documentTypeName,
            document.FileName,
            document.FilePath,
            document.ContentType,
            document.UploadedDate);
    }
}
