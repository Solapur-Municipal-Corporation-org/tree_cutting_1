"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ChangeEvent, FormEvent, useEffect, useState } from "react";
import styles from "../../citizen.module.css";
import { citizenFetch, DocumentRequirement, MasterOption, ApplicationDocument, ApplicationPhoto, responseMessage, citizenFileUrl } from "../../citizen-api";

type FormState = {
  applicationTypeId: string; applicantTypeId: string; fullName: string; address: string; emailId: string; mobileNo: string; aadharNo: string;
  petName: string; pethNo: string; zoneNo: string; prabhagNo: string; propertyTaxNo: string; treeAddress: string;
  treeCuttingReason: string; numberOfTreeCutting: string; treeSpecies: string;
};
type Errors = Partial<Record<keyof FormState, string>> & { documents?: string; photos?: string };
type Draft = { applicationId: number; applicationNumber: string };
const initialForm: FormState = { applicationTypeId: "", applicantTypeId: "", fullName: "", address: "", emailId: "", mobileNo: "", aadharNo: "", petName: "", pethNo: "", zoneNo: "", prabhagNo: "", propertyTaxNo: "", treeAddress: "", treeCuttingReason: "", numberOfTreeCutting: "", treeSpecies: "" };

export default function CitizenTreeCuttingApplyPage() {
  const [form, setForm] = useState<FormState>(initialForm);
  const [applicationTypes, setApplicationTypes] = useState<MasterOption[]>([]);
  const [applicantTypes, setApplicantTypes] = useState<MasterOption[]>([]);
  const [zones, setZones] = useState<MasterOption[]>([]);
  const [peths, setPeths] = useState<MasterOption[]>([]);
  const [prabhags, setPrabhags] = useState<MasterOption[]>([]);
  const [documents, setDocuments] = useState<DocumentRequirement[]>([]);
  const [uploadedDocuments, setUploadedDocuments] = useState<ApplicationDocument[]>([]);
  const [photos, setPhotos] = useState<ApplicationPhoto[]>([]);
  const [draft, setDraft] = useState<Draft | null>(null);
  const [step, setStep] = useState(1);
  const [errors, setErrors] = useState<Errors>({});
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const router = useRouter();

  useEffect(() => {
    Promise.all([
      citizenFetch("/api/citizen/profile"), citizenFetch("/api/masters/application-types"), citizenFetch("/api/masters/applicant-types"),
      citizenFetch("/api/masters/zones"), citizenFetch("/api/masters/peths"), citizenFetch("/api/masters/prabhags")
    ]).then(async (responses) => {
      const failed = responses.find((response) => !response.ok);
      if (failed) throw new Error(await responseMessage(failed, "Unable to load application data."));
      const [profileData, applicationTypesData, applicantTypesData, zonesData, pethsData, prabhagsData] = await Promise.all(responses.map((response) => response.json()));
      setApplicationTypes(applicationTypesData); setApplicantTypes(applicantTypesData); setZones(zonesData); setPeths(pethsData); setPrabhags(prabhagsData);
      setForm((current) => ({ ...current, fullName: profileData.name || current.fullName, address: profileData.address || current.address, emailId: profileData.email || current.emailId, mobileNo: profileData.mobileNumber || current.mobileNo }));
    }).catch((error: unknown) => setMessage(error instanceof Error ? error.message : "Unable to load application data."));
  }, []);

  useEffect(() => {
    if (!form.applicationTypeId) return;
    citizenFetch(`/api/masters/documents/application-type/${form.applicationTypeId}`).then(async (response) => {
      if (!response.ok) throw new Error(await responseMessage(response, "Unable to load document requirements."));
      setDocuments(await response.json());
    }).catch((error: unknown) => setMessage(error instanceof Error ? error.message : "Unable to load document requirements."));
  }, [form.applicationTypeId]);

  const update = (field: keyof FormState, value: string) => { setForm((current) => ({ ...current, [field]: value })); if (field === "applicationTypeId" && !value) setDocuments([]); setErrors((current) => ({ ...current, [field]: undefined })); };
  const validate = (section: 1 | 2 | 3 | 4) => {
    const next: Errors = {};
    if (section === 1) {
      if (!form.applicationTypeId) next.applicationTypeId = "Application type is required.";
      if (!form.applicantTypeId) next.applicantTypeId = "Applicant type is required.";
      if (!form.fullName.trim()) next.fullName = "Full name is required.";
      if (!form.address.trim()) next.address = "Address is required.";
      if (!form.emailId.trim()) next.emailId = "Email ID is required.";
      if (!form.mobileNo.trim()) next.mobileNo = "Mobile number is required.";
      if (!form.aadharNo.trim()) next.aadharNo = "Aadhar number is required.";
      if (!form.petName.trim()) next.petName = "Peth name is required.";
      if (!form.pethNo.trim()) next.pethNo = "Peth number is required.";
      if (!form.zoneNo.trim()) next.zoneNo = "Zone number is required.";
      if (!form.prabhagNo.trim()) next.prabhagNo = "Prabhag number is required.";
      if (!form.propertyTaxNo.trim()) next.propertyTaxNo = "Property tax number is required.";
    }
    if (section === 2) {
      if (!form.treeAddress.trim()) next.treeAddress = "Tree address is required.";
      if (!form.treeCuttingReason.trim()) next.treeCuttingReason = "Tree cutting reason is required.";
      if (!form.numberOfTreeCutting.trim()) next.numberOfTreeCutting = "Number of trees is required.";
      if (!form.treeSpecies.trim()) next.treeSpecies = "Tree species is required.";
    }
    if (section === 3 && documents.some((document) => document.isRequired && !uploadedDocuments.some((upload) => upload.documentTypeId === document.documentTypeId))) next.documents = "Please upload all required documents.";
    if (section === 4 && photos.length === 0) next.photos = "At least one tree photograph is required.";
    setErrors(next); return Object.keys(next).length === 0;
  };

  const createDraft = async () => {
    const response = await citizenFetch("/api/citizen/applications", { method: "POST", body: JSON.stringify({ ...form, applicationTypeId: Number(form.applicationTypeId), applicantTypeId: Number(form.applicantTypeId), numberOfTreeCutting: Number(form.numberOfTreeCutting) }) });
    if (!response.ok) throw new Error(await responseMessage(response, "Unable to create your application draft."));
    const created = await response.json() as Draft; setDraft(created); return created;
  };

  const goNext = async () => {
    if (!validate(step as 1 | 2 | 3 | 4)) return;
    setMessage(null); setBusy(true);
    try {
      if (step === 2 && !draft) await createDraft();
      setStep((current) => Math.min(current + 1, 5));
    } catch (error: unknown) { setMessage(error instanceof Error ? error.message : "Unable to continue."); }
    finally { setBusy(false); }
  };

  const uploadDocument = async (document: DocumentRequirement, event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]; if (!file || !draft) return;
    if (file.size > 5 * 1024 * 1024) { setErrors((current) => ({ ...current, documents: "Each document must be 5 MB or less." })); return; }
    const data = new FormData(); data.append("file", file); data.append("applicationTypeId", form.applicationTypeId); data.append("documentTypeId", String(document.documentTypeId));
    const response = await citizenFetch(`/api/citizen/applications/${draft.applicationId}/documents/upload`, { method: "POST", body: data });
    if (!response.ok) { setMessage(await responseMessage(response, "Document upload failed.")); return; }
    const uploaded = await response.json() as ApplicationDocument;
    setUploadedDocuments((current) => [...current.filter((item) => item.documentTypeId !== uploaded.documentTypeId), uploaded]);
    setErrors((current) => ({ ...current, documents: undefined }));
  };

  const uploadPhoto = async (event: ChangeEvent<HTMLInputElement>, photoId?: number) => {
    const file = event.target.files?.[0]; if (!file || !draft) return;
    if (file.size > 5 * 1024 * 1024 || !["image/jpeg", "image/png"].includes(file.type)) { setErrors((current) => ({ ...current, photos: "Photos must be JPG, JPEG, or PNG and 5 MB or less." })); return; }
    const data = new FormData(); data.append("file", file); if (photoId) data.append("photoId", String(photoId));
    const response = await citizenFetch(`/api/citizen/applications/${draft.applicationId}/photos/upload`, { method: "POST", body: data });
    if (!response.ok) { setMessage(await responseMessage(response, "Photo upload failed.")); return; }
    const uploaded = await response.json() as ApplicationPhoto;
    setPhotos((current) => [...current.filter((item) => item.applicationPhotoId !== uploaded.applicationPhotoId), uploaded]);
    setErrors((current) => ({ ...current, photos: undefined }));
  };

  const deletePhoto = async (photoId: number) => {
    if (!draft) return;
    const response = await citizenFetch(`/api/citizen/applications/${draft.applicationId}/photos/${photoId}`, { method: "DELETE" });
    if (response.ok) setPhotos((current) => current.filter((photo) => photo.applicationPhotoId !== photoId));
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault(); if (!validate(4) || !draft) return; setBusy(true); setMessage(null);
    try {
      const response = await citizenFetch(`/api/citizen/applications/${draft.applicationId}/submit`, { method: "POST" });
      if (!response.ok) throw new Error(await responseMessage(response, "Application submission failed."));
      router.push(`/citizen/tree-cutting/application/${draft.applicationId}?submitted=1`);
    } catch (error: unknown) { setMessage(error instanceof Error ? error.message : "Application submission failed."); }
    finally { setBusy(false); }
  };

  const field = (name: keyof FormState, label: string, type = "text", wide = false) => (
    <label className={`${styles.field} ${wide ? styles.wide : ""}`}><span>{label}</span><input type={type} value={form[name]} onChange={(event) => update(name, event.target.value)} />{errors[name] && <small className={styles.error}>{errors[name]}</small>}</label>
  );

  return (
    <>
      <div className={styles.eyebrow}>Tree cutting service</div><h1>New application</h1>
      <p className={styles.lede}>Your authenticated SMC profile is used for applicant contact details. Review every section before submitting.</p>
      {message && <div className={styles.alert}>{message}</div>}
      <div className={styles.progress}>{["Applicant and property", "Tree details", "Documents", "Photographs", "Preview"].map((label, index) => <span key={label} className={step >= index + 1 ? styles.active : ""}>{index + 1}. {label}</span>)}</div>
      <form onSubmit={submit} className={styles.card}>
        {step === 1 && <>
          <h2>Applicant and property information</h2><div className={styles.formGrid}>
            <label className={styles.field}><span>Application type</span><select value={form.applicationTypeId} onChange={(event) => update("applicationTypeId", event.target.value)}><option value="">Select application type</option>{applicationTypes.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>{errors.applicationTypeId && <small className={styles.error}>{errors.applicationTypeId}</small>}</label>
            <label className={styles.field}><span>Applicant type</span><select value={form.applicantTypeId} onChange={(event) => update("applicantTypeId", event.target.value)}><option value="">Select applicant type</option>{applicantTypes.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>{errors.applicantTypeId && <small className={styles.error}>{errors.applicantTypeId}</small>}</label>
            {field("fullName", "Full name")} {field("emailId", "Email ID", "email")} {field("mobileNo", "Mobile number", "tel")} {field("aadharNo", "Aadhar number")}
            {field("address", "Address", "text", true)} {field("petName", "Peth name")} 
            <label className={styles.field}><span>Peth number</span><select value={form.pethNo} onChange={(event) => update("pethNo", event.target.value)}><option value="">Select peth</option>{peths.map((item) => <option key={item.id} value={item.name}>{item.name}</option>)}</select></label>
            <label className={styles.field}><span>Zone number</span><select value={form.zoneNo} onChange={(event) => update("zoneNo", event.target.value)}><option value="">Select zone</option>{zones.map((item) => <option key={item.id} value={item.name}>{item.name}</option>)}</select></label>
            <label className={styles.field}><span>Prabhag number</span><select value={form.prabhagNo} onChange={(event) => update("prabhagNo", event.target.value)}><option value="">Select prabhag</option>{prabhags.map((item) => <option key={item.id} value={item.name}>{item.name}</option>)}</select></label>
            {field("propertyTaxNo", "Property tax number")}
          </div>
        </>}
        {step === 2 && <><h2>Tree information</h2><div className={styles.formGrid}>{field("treeAddress", "Tree address", "text", true)}{field("treeCuttingReason", "Tree cutting reason", "text", true)}{field("numberOfTreeCutting", "Number of trees", "number")}{field("treeSpecies", "Tree species")}</div></>}
        {step === 3 && <><h2>Required documents</h2><div className={styles.uploadList}>{documents.map((document) => { const uploaded = uploadedDocuments.find((item) => item.documentTypeId === document.documentTypeId); return <div className={styles.uploadRow} key={document.documentTypeId}><span>{document.documentTypeName}{document.isRequired ? " *" : ""}{uploaded ? ` - ${uploaded.fileName}` : ""}</span><input type="file" accept={document.documentTypeName.toLowerCase().includes("photo") ? ".jpg,.jpeg,.png" : ".pdf"} onChange={(event) => uploadDocument(document, event)} /></div>; })}</div>{errors.documents && <p className={styles.error}>{errors.documents}</p>}</>}
        {step === 4 && <><h2>Tree photographs</h2><p className={styles.lede}>Add photographs from different angles. Each image must be JPG, JPEG, or PNG and 5 MB or less.</p><div className={styles.photoGrid}>{photos.map((photo) => <article className={styles.photoCard} key={photo.applicationPhotoId}><img src={citizenFileUrl(`/api/citizen/applications/${draft?.applicationId}/photos/${photo.applicationPhotoId}/file`)} alt={photo.fileName} /><div><label className={styles.cardLink}>Replace<input hidden type="file" accept=".jpg,.jpeg,.png" onChange={(event) => uploadPhoto(event, photo.applicationPhotoId)} /></label><button type="button" className={styles.buttonSecondary} onClick={() => deletePhoto(photo.applicationPhotoId)}>Delete</button></div></article>)}</div><label className={styles.buttonSecondary} style={{ marginTop: 18, display: "inline-block" }}>Add photo<input hidden type="file" accept=".jpg,.jpeg,.png" onChange={(event) => uploadPhoto(event)} /></label>{errors.photos && <p className={styles.error}>{errors.photos}</p>}</>}
        {step === 5 && <><h2>Preview application</h2><dl className={styles.detailGrid}>{Object.entries({ "Application type": applicationTypes.find((item) => String(item.id) === form.applicationTypeId)?.name, "Applicant type": applicantTypes.find((item) => String(item.id) === form.applicantTypeId)?.name, "Full name": form.fullName, Address: form.address, "Email ID": form.emailId, "Mobile number": form.mobileNo, "Aadhar number": form.aadharNo, "Peth name": form.petName, "Peth number": form.pethNo, Zone: form.zoneNo, Prabhag: form.prabhagNo, "Property tax number": form.propertyTaxNo, "Tree address": form.treeAddress, "Tree cutting reason": form.treeCuttingReason, "Number of trees": form.numberOfTreeCutting, "Tree species": form.treeSpecies }).map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || "-"}</dd></div>)}</dl><h3>Documents</h3><p>{uploadedDocuments.map((document) => document.fileName).join(", ") || "No documents uploaded"}</p><h3>Photographs</h3><p>{photos.length} photograph{photos.length === 1 ? "" : "s"} uploaded.</p></>}
        <div className={styles.actions}><Link className={styles.buttonSecondary} href="/citizen/dashboard">Cancel</Link>{step > 1 && <button type="button" className={styles.buttonSecondary} onClick={() => setStep((current) => current - 1)}>Back</button>}{step < 5 && <button type="button" className={styles.button} disabled={busy} onClick={goNext}>{busy ? "Working..." : "Continue"}</button>}{step === 5 && <button type="submit" className={styles.button} disabled={busy}>{busy ? "Submitting..." : "Submit application"}</button>}</div>
      </form>
    </>
  );
}
