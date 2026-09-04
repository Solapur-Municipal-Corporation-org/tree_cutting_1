"use client";

import { useEffect, useMemo, useState } from "react";
import styles from "./page.module.css";

type MasterOption = { id: number; name: string };
type DocumentOption = { documentTypeId: number; documentTypeName: string; isRequired: boolean; displayOrder: number };
type FormState = {
  applicationTypeId: string;
  applicantTypeId: string;
  fullName: string;
  address: string;
  emailId: string;
  mobileNo: string;
  aadharNo: string;
  petName: string;
  pethNo: string;
  zoneNo: string;
  prabhagNo: string;
  propertyTaxNo: string;
  treeAddress: string;
  treeCuttingReason: string;
  numberOfTreeCutting: string;
  treeSpecies: string;
};

type ErrorState = Partial<Record<keyof FormState, string>> & { documents?: string };

type UploadMap = Record<string, File | null>;

type SubmittedApplication = {
  applicationId: number;
  applicationNumber: string;
} | null;

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5216";
const MAX_DOCUMENT_SIZE_BYTES = 5 * 1024 * 1024;

const initialFormState: FormState = {
  applicationTypeId: "",
  applicantTypeId: "",
  fullName: "",
  address: "",
  emailId: "",
  mobileNo: "",
  aadharNo: "",
  petName: "",
  pethNo: "",
  zoneNo: "",
  prabhagNo: "",
  propertyTaxNo: "",
  treeAddress: "",
  treeCuttingReason: "",
  numberOfTreeCutting: "",
  treeSpecies: "",
};

export default function Home() {
  const [applicationTypes, setApplicationTypes] = useState<MasterOption[]>([]);
  const [applicantTypes, setApplicantTypes] = useState<MasterOption[]>([]);
  const [zones, setZones] = useState<MasterOption[]>([]);
  const [peths, setPeths] = useState<MasterOption[]>([]);
  const [prabhags, setPrabhags] = useState<MasterOption[]>([]);
  const [documentRequirements, setDocumentRequirements] = useState<DocumentOption[]>([]);
  const [form, setForm] = useState<FormState>(initialFormState);
  const [errors, setErrors] = useState<ErrorState>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [pendingUploads, setPendingUploads] = useState<UploadMap>({});
  const [currentStep, setCurrentStep] = useState<1 | 2 | 3>(1);
  const [submittedApplication, setSubmittedApplication] = useState<SubmittedApplication>(null);

  useEffect(() => {
    const fetchMasters = async () => {
      try {
        const [applicationTypeRes, applicantTypeRes, zonesRes, pethsRes, prabhagRes] = await Promise.all([
          fetch(`${API_BASE_URL}/api/masters/application-types`),
          fetch(`${API_BASE_URL}/api/masters/applicant-types`),
          fetch(`${API_BASE_URL}/api/masters/zones`),
          fetch(`${API_BASE_URL}/api/masters/peths`),
          fetch(`${API_BASE_URL}/api/masters/prabhags`),
        ]);

        if (!applicationTypeRes.ok || !applicantTypeRes.ok || !zonesRes.ok || !pethsRes.ok || !prabhagRes.ok) {
          throw new Error("Failed to load master data.");
        }

        const [applicationTypeData, applicantTypeData, zonesData, pethsData, prabhagData] = await Promise.all([
          applicationTypeRes.json(),
          applicantTypeRes.json(),
          zonesRes.json(),
          pethsRes.json(),
          prabhagRes.json(),
        ]);

        setApplicationTypes(applicationTypeData);
        setApplicantTypes(applicantTypeData);
        setZones(zonesData);
        setPeths(pethsData);
        setPrabhags(prabhagData);
      } catch (error) {
        setStatusMessage(error instanceof Error ? error.message : "Unable to load masters.");
      }
    };

    fetchMasters();
  }, []);

  useEffect(() => {
    const applicationTypeId = Number(form.applicationTypeId);
    if (!applicationTypeId) {
      setDocumentRequirements([]);
      setPendingUploads({});
      return;
    }

    const fetchDocuments = async () => {
      setLoading(true);
      try {
        const response = await fetch(`${API_BASE_URL}/api/masters/documents/application-type/${applicationTypeId}`);
        if (!response.ok) {
          throw new Error("Unable to load document requirements.");
        }
        const data: DocumentOption[] = await response.json();
        setDocumentRequirements(data);
        setPendingUploads((prev) => {
          const next: UploadMap = {};
          data.forEach((doc) => {
            next[String(doc.documentTypeId)] = prev[String(doc.documentTypeId)] ?? null;
          });
          return next;
        });
      } catch (error) {
        setStatusMessage(error instanceof Error ? error.message : "Unable to load required documents.");
      } finally {
        setLoading(false);
      }
    };

    fetchDocuments();
  }, [form.applicationTypeId]);

  const applicationTypeTitle = useMemo(
    () => applicationTypes.find((item) => String(item.id) === form.applicationTypeId)?.name ?? "",
    [applicationTypes, form.applicationTypeId],
  );

  const updateField = (field: keyof FormState, value: string) => {
    setForm((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: "" }));
  };

  const validateStep1 = () => {
    const nextErrors: ErrorState = {};

    if (!form.applicationTypeId) nextErrors.applicationTypeId = "Application type is required.";
    if (!form.applicantTypeId) nextErrors.applicantTypeId = "Applicant type is required.";
    if (!form.fullName.trim()) nextErrors.fullName = "Full name is required.";
    if (!form.address.trim()) nextErrors.address = "Address is required.";
    if (!form.emailId.trim()) nextErrors.emailId = "Email ID is required.";
    if (!form.mobileNo.trim()) nextErrors.mobileNo = "Mobile number is required.";
    if (!form.aadharNo.trim()) nextErrors.aadharNo = "Aadhar number is required.";
    if (!form.petName.trim()) nextErrors.petName = "Peth Name is required.";
    if (!form.pethNo.trim()) nextErrors.pethNo = "Peth number is required.";
    if (!form.zoneNo.trim()) nextErrors.zoneNo = "Zone number is required.";
    if (!form.prabhagNo.trim()) nextErrors.prabhagNo = "Prabhag number is required.";
    if (!form.propertyTaxNo.trim()) nextErrors.propertyTaxNo = "Property tax number is required.";

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const validateStep2 = () => {
    const nextErrors: ErrorState = {};

    if (!form.treeAddress.trim()) nextErrors.treeAddress = "Tree address is required.";
    if (!form.treeCuttingReason.trim()) nextErrors.treeCuttingReason = "Tree cutting reason is required.";
    if (!form.numberOfTreeCutting.trim()) nextErrors.numberOfTreeCutting = "Number of trees is required.";
    if (!form.treeSpecies.trim()) nextErrors.treeSpecies = "Tree species is required.";

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const validateStep3 = () => {
    const nextErrors: ErrorState = {};

    if (documentRequirements.length > 0) {
      const missingDocs = documentRequirements.some((document) => !pendingUploads[String(document.documentTypeId)]);
      if (missingDocs) {
        nextErrors.documents = "Please upload all required documents for the selected application type.";
      }
    }

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const validateForm = () => {
    const step1Valid = validateStep1();
    if (!step1Valid) return false;

    const step2Valid = validateStep2();
    if (!step2Valid) return false;

    return validateStep3();
  };

  const resetFormState = () => {
    setForm(initialFormState);
    setErrors({});
    setDocumentRequirements([]);
    setPendingUploads({});
    setCurrentStep(1);
    setStatusMessage(null);
    setSubmittedApplication(null);
  };

  const handleNextStep = () => {
    if (currentStep === 1) {
      if (validateStep1()) {
        setCurrentStep(2);
      }
      return;
    }

    if (currentStep === 2) {
      if (validateStep2()) {
        setCurrentStep(3);
      }
    }
  };

  const handlePreviousStep = () => {
    setCurrentStep((current) => (current > 1 ? (current - 1) as 1 | 2 | 3 : 1));
    setErrors({});
  };

  const handleNewApplication = () => {
    resetFormState();
  };

  const handleDocumentUpload = (documentTypeId: number, file: File | null) => {
    if (file && file.size > MAX_DOCUMENT_SIZE_BYTES) {
      setPendingUploads((current) => ({ ...current, [String(documentTypeId)]: null }));
      setErrors((current) => ({ ...current, documents: "Each document must be 5 MB or less." }));
      return;
    }

    setPendingUploads((current) => ({ ...current, [String(documentTypeId)]: file }));
    setErrors((current) => ({ ...current, documents: "" }));
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);
    setStatusMessage(null);

    try {
      const submittedPayload = {
        applicationTypeId: Number(form.applicationTypeId),
        applicantTypeId: Number(form.applicantTypeId),
        fullName: form.fullName,
        address: form.address,
        emailId: form.emailId,
        mobileNo: form.mobileNo,
        aadharNo: form.aadharNo,
        petName: form.petName,
        pethNo: form.pethNo,
        zoneNo: form.zoneNo,
        prabhagNo: form.prabhagNo,
        propertyTaxNo: form.propertyTaxNo,
        treeAddress: form.treeAddress,
        treeCuttingReason: form.treeCuttingReason,
        numberOfTreeCutting: Number(form.numberOfTreeCutting),
        treeSpecies: form.treeSpecies,
      };

      const applicationResponse = await fetch(`${API_BASE_URL}/api/applications`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(submittedPayload),
      });

      if (!applicationResponse.ok) {
        const message = await applicationResponse.text();
        throw new Error(message || "Unable to create form application.");
      }

      const createdApplication = (await applicationResponse.json()) as {
        applicationId?: number;
        applicationNumber?: string;
      };
      if (!createdApplication.applicationId || !createdApplication.applicationNumber) {
        throw new Error("The API returned an invalid application response.");
      }
      const applicationId = createdApplication.applicationId;
      const applicationNumber = createdApplication.applicationNumber;

      for (const document of documentRequirements) {
        const file = pendingUploads[String(document.documentTypeId)];
        if (!file) {
          continue;
        }

        const formData = new FormData();
        formData.append("file", file);
        formData.append("applicationTypeId", String(form.applicationTypeId));
        formData.append("documentTypeId", String(document.documentTypeId));

        const uploadResponse = await fetch(`${API_BASE_URL}/api/applications/${applicationId}/documents/upload`, {
          method: "POST",
          body: formData,
        });

        if (!uploadResponse.ok) {
          const responseMessage = await uploadResponse.text();
          let message = responseMessage;
          try {
            const errorBody = JSON.parse(responseMessage) as { message?: string };
            message = errorBody.message ?? responseMessage;
          } catch {
            // Keep the plain response when the API does not return JSON.
          }
          throw new Error(message || `Document upload failed for ${document.documentTypeName}.`);
        }
      }

      const submitResponse = await fetch(`${API_BASE_URL}/api/applications/${applicationId}/submit`, {
        method: "POST",
      });

      if (!submitResponse.ok) {
        const responseMessage = await submitResponse.text();
        let message = responseMessage;
        try {
          const errorBody = JSON.parse(responseMessage) as { message?: string };
          message = errorBody.message ?? responseMessage;
        } catch {
          // Keep the plain response when the API does not return JSON.
        }
        throw new Error(message || "Application submission failed.");
      }

      setSubmittedApplication({
        applicationId,
        applicationNumber,
      });
      setStatusMessage(`Application submitted successfully! Your application number is: ${applicationNumber}`);
    } catch (error) {
      setStatusMessage(error instanceof Error ? error.message : "Submission failed.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className={styles.pageShell}>
      <div className={styles.formCard}>
        <div className={styles.headerBanner}>
          <h1>Tree Cutting Application Form</h1>
        </div>

        {submittedApplication ? (
          <div className={styles.successScreen}>
            <div className={styles.successCard}>
              <div className={styles.successBadge}>Application Submitted</div>
              <h2>Your Application Number</h2>
              <div className={styles.applicationNumber}>{submittedApplication.applicationNumber}</div>
              <p className={styles.successText}>Your application has been submitted successfully. Please keep this number for future reference.</p>

              <button type="button" className={styles.newApplicationButton} onClick={handleNewApplication}>
                New Application
              </button>
            </div>
          </div>
        ) : (
          <>
            {/* Step Progress Indicator */}
            <div className={styles.stepProgress}>
              <div className={`${styles.stepIndicator} ${currentStep >= 1 ? styles.stepActive : ""}`}>
                <div className={styles.stepNumber}>1</div>
                <div className={styles.stepLabel}>Application Info</div>
              </div>
              <div className={styles.stepConnector}></div>
              <div className={`${styles.stepIndicator} ${currentStep >= 2 ? styles.stepActive : ""}`}>
                <div className={styles.stepNumber}>2</div>
                <div className={styles.stepLabel}>Tree Details</div>
              </div>
              <div className={styles.stepConnector}></div>
              <div className={`${styles.stepIndicator} ${currentStep >= 3 ? styles.stepActive : ""}`}>
                <div className={styles.stepNumber}>3</div>
                <div className={styles.stepLabel}>Document Upload</div>
              </div>
            </div>

            <form onSubmit={handleSubmit} className={styles.form} noValidate>
          {/* Step 1: Application Details */}
          {currentStep === 1 && (
            <>
              <section className={styles.section}>
                <div className={styles.sectionHeader}>Application Details</div>
                <div className={styles.fieldGridSingle}>
                  <label className={styles.fieldWrap}>
                    <span>Application Type</span>
                    <select
                      value={form.applicationTypeId}
                      onChange={(e) => updateField("applicationTypeId", e.target.value)}
                      className={errors.applicationTypeId ? styles.inputError : ""}
                    >
                      <option value="">Select Application Type</option>
                      {applicationTypes.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                    {errors.applicationTypeId ? <small>{errors.applicationTypeId}</small> : null}
                  </label>
                </div>
              </section>

              <section className={styles.section}>
                <div className={styles.sectionHeader}>Applicant Details</div>

                <div className={styles.fieldGridFour}>
                  <label className={styles.fieldWrap}>
                    <span>Applicant Type</span>
                    <select
                      value={form.applicantTypeId}
                      onChange={(e) => updateField("applicantTypeId", e.target.value)}
                      className={errors.applicantTypeId ? styles.inputError : ""}
                    >
                      <option value="">Select Applicant Type</option>
                      {applicantTypes.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                    {errors.applicantTypeId ? <small>{errors.applicantTypeId}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Full Name</span>
                    <input value={form.fullName} onChange={(e) => updateField("fullName", e.target.value)} className={errors.fullName ? styles.inputError : ""} placeholder="Enter full name" />
                    {errors.fullName ? <small>{errors.fullName}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Address</span>
                    <textarea value={form.address} onChange={(e) => updateField("address", e.target.value)} className={errors.address ? styles.inputError : ""} placeholder="Enter address" rows={3} />
                    {errors.address ? <small>{errors.address}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Email ID</span>
                    <input 
                      type="email" 
                      value={form.emailId} 
                      onChange={(e) => updateField("emailId", e.target.value)} 
                      className={errors.emailId ? styles.inputError : ""} 
                      placeholder="Enter email (e.g., name@example.com)"
                      pattern="^[^\s@]+@[^\s@]+\.[^\s@]+$"
                      title="Please enter a valid email address"
                    />
                    {errors.emailId ? <small>{errors.emailId}</small> : null}
                  </label>
                </div>

                <div className={styles.fieldGridFour}>
                  <label className={styles.fieldWrap}>
                    <span>Mobile No</span>
                    <input 
                      id="txtmobilnumber"
                      name="txtmobilnumber"
                      type="tel" 
                      inputMode="numeric" 
                      value={form.mobileNo} 
                      onChange={(e) => {
                        const val = e.target.value.replace(/[^0-9]/g, "").slice(0, 10);
                        updateField("mobileNo", val);
                      }}
                      className={errors.mobileNo ? styles.inputError : ""} 
                      placeholder="10-digit mobile number"
                      pattern="^[6-9]\d{9}$"
                      title="Mobile number must be 10 digits and start with 6-9"
                      maxLength={10}
                    />
                    {errors.mobileNo ? <small>{errors.mobileNo}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Aadhar No</span>
                    <input 
                      type="text" 
                      inputMode="numeric" 
                      value={form.aadharNo} 
                      onChange={(e) => {
                        const val = e.target.value.replace(/[^0-9]/g, "").slice(0, 12);
                        updateField("aadharNo", val);
                      }}
                      className={errors.aadharNo ? styles.inputError : ""} 
                      placeholder="12-digit Aadhar number"
                      pattern="^\d{12}$"
                      title="Aadhar number must be exactly 12 digits"
                      maxLength={12}
                    />
                    {errors.aadharNo ? <small>{errors.aadharNo}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Peth Name</span>
                    <input value={form.petName} onChange={(e) => updateField("petName", e.target.value)} className={errors.petName ? styles.inputError : ""} placeholder="Enter Peth Name" />
                    {errors.petName ? <small>{errors.petName}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Peth No</span>
                    <select value={form.pethNo} onChange={(e) => updateField("pethNo", e.target.value)} className={errors.pethNo ? styles.inputError : ""}>
                      <option value="">Select Peth</option>
                      {peths.map((item) => (
                        <option key={item.id} value={item.name}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                    {errors.pethNo ? <small>{errors.pethNo}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Zone No</span>
                    <select value={form.zoneNo} onChange={(e) => updateField("zoneNo", e.target.value)} className={errors.zoneNo ? styles.inputError : ""}>
                      <option value="">Select Zone</option>
                      {zones.map((item) => (
                        <option key={item.id} value={item.name}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                    {errors.zoneNo ? <small>{errors.zoneNo}</small> : null}
                  </label>
                </div>

                <div className={styles.fieldGridFour}>
                  <label className={styles.fieldWrap}>
                    <span>Prabhag No</span>
                    <select value={form.prabhagNo} onChange={(e) => updateField("prabhagNo", e.target.value)} className={errors.prabhagNo ? styles.inputError : ""}>
                      <option value="">Select Prabhag</option>
                      {prabhags.map((item) => (
                        <option key={item.id} value={item.name}>
                          {item.name}
                        </option>
                      ))}
                    </select>
                    {errors.prabhagNo ? <small>{errors.prabhagNo}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Property Tax No</span>
                    <input value={form.propertyTaxNo} onChange={(e) => updateField("propertyTaxNo", e.target.value)} className={errors.propertyTaxNo ? styles.inputError : ""} placeholder="Enter property tax number" />
                    {errors.propertyTaxNo ? <small>{errors.propertyTaxNo}</small> : null}
                  </label>
                </div>
              </section>

              {statusMessage ? <div className={styles.statusMessage}>{statusMessage}</div> : null}

              <div className={styles.buttonRow}>
                <button
                  type="button"
                  className={styles.resetButton}
                  onClick={() => {
                    setForm(initialFormState);
                    setErrors({});
                    setPendingUploads({});
                    setDocumentRequirements([]);
                    setStatusMessage(null);
                    setCurrentStep(1);
                  }}
                >
                  Reset
                </button>
                <button type="button" className={styles.nextButton} onClick={handleNextStep}>
                  Next
                </button>
              </div>
            </>
          )}

          {/* Step 2: Tree Details */}
          {currentStep === 2 && (
            <>
              <section className={styles.section}>
                <div className={styles.sectionHeader}>Tree Details</div>

                <div className={styles.fieldGridFour}>
                  <label className={styles.fieldWrap}>
                    <span>Tree Address</span>
                    <textarea value={form.treeAddress} onChange={(e) => updateField("treeAddress", e.target.value)} className={errors.treeAddress ? styles.inputError : ""} placeholder="Enter tree address" rows={3} />
                    {errors.treeAddress ? <small>{errors.treeAddress}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Tree Cutting Reason</span>
                    <input value={form.treeCuttingReason} onChange={(e) => updateField("treeCuttingReason", e.target.value)} className={errors.treeCuttingReason ? styles.inputError : ""} placeholder="Enter reason" />
                    {errors.treeCuttingReason ? <small>{errors.treeCuttingReason}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Number of Tree Cutting</span>
                    <input type="number" min="1" value={form.numberOfTreeCutting} onChange={(e) => updateField("numberOfTreeCutting", e.target.value)} className={errors.numberOfTreeCutting ? styles.inputError : ""} placeholder="1" />
                    {errors.numberOfTreeCutting ? <small>{errors.numberOfTreeCutting}</small> : null}
                  </label>

                  <label className={styles.fieldWrap}>
                    <span>Tree Species</span>
                    <input value={form.treeSpecies} onChange={(e) => updateField("treeSpecies", e.target.value)} className={errors.treeSpecies ? styles.inputError : ""} placeholder="Enter tree species" />
                    {errors.treeSpecies ? <small>{errors.treeSpecies}</small> : null}
                  </label>
                </div>
              </section>

              {statusMessage ? <div className={styles.statusMessage}>{statusMessage}</div> : null}

              <div className={styles.buttonRow}>
                <button type="button" className={styles.previousButton} onClick={handlePreviousStep}>
                  Previous
                </button>
                <button type="button" className={styles.nextButton} onClick={handleNextStep}>
                  Next
                </button>
              </div>
            </>
          )}

          {/* Step 3: Document Upload */}
          {currentStep === 3 && (
            <>
              <section className={styles.section}>
                <div className={styles.sectionHeader}>Document Upload</div>
                {applicationTypeTitle ? (
                  <div className={styles.applicationTypeNote}>Required documents for: {applicationTypeTitle}</div>
                ) : null}

                <div className={styles.applicationTypeNote}>
                  <a href="/Hamipatra.pdf" target="_blank" rel="noopener noreferrer" download="Hamipatra.pdf">
                    Download Hamipatra PDF
                  </a>
                </div>

                {loading ? <div className={styles.loadingText}>Loading document requirements...</div> : null}

                {documentRequirements.length > 0 ? (
                  <div className={styles.documentTableWrap}>
                    <table className={styles.documentTable}>
                      <thead>
                        <tr>
                          <th>Sr. No.</th>
                          <th>Document Name</th>
                          <th>File Upload</th>
                        </tr>
                      </thead>
                      <tbody>
                        {documentRequirements.map((document, index) => (
                          <tr key={document.documentTypeId}>
                            <td>{index + 1}</td>
                            <td>{document.documentTypeName}</td>
                            <td>
                              <input
                                type="file"
                                onChange={(e) => handleDocumentUpload(document.documentTypeId, e.target.files?.[0] ?? null)}
                                accept={
                                  document.documentTypeName.toLowerCase().includes("photo")
                                    ? ".jpg,.jpeg,.png"
                                    : ".pdf"
                                }
                              />
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className={styles.emptyState}>Select an application type to load the required document list.</div>
                )}

                {errors.documents ? <div className={styles.errorMessage}>{errors.documents}</div> : null}
              </section>

              {statusMessage ? <div className={styles.statusMessage}>{statusMessage}</div> : null}

              <div className={styles.buttonRow}>
                <button type="button" className={styles.previousButton} onClick={handlePreviousStep}>
                  Previous
                </button>
                <button type="submit" className={styles.submitButton} disabled={isSubmitting}>
                  {isSubmitting ? "Submitting..." : "Submit Application"}
                </button>
              </div>
            </>
          )}
            </form>
          </>
        )}
      </div>
    </div>
  );
}
