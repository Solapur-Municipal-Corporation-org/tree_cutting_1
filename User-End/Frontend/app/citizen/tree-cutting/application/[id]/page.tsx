"use client";

import Link from "next/link";
import { use, useEffect, useState } from "react";
import styles from "../../../citizen.module.css";
import { ApplicationDetail, citizenFetch, citizenFileUrl, formatDate, responseMessage } from "../../../citizen-api";

export default function CitizenApplicationDetailsPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const [application, setApplication] = useState<ApplicationDetail | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    citizenFetch(`/api/citizen/applications/${id}`).then(async (response) => {
      if (!response.ok) throw new Error(await responseMessage(response, "Application not found."));
      setApplication(await response.json());
    }).catch((error: unknown) => setMessage(error instanceof Error ? error.message : "Unable to load application."));
  }, [id]);

  if (message) return <div className={styles.alert}>{message}</div>;
  if (!application) return <p className={styles.loading}>Loading application...</p>;

  return (
    <>
      {application.isSubmitted && <section className={styles.card} style={{ borderColor: "#9dceb9", background: "#f4fbf7", marginBottom: 24 }}><div className={styles.eyebrow}>Application submitted successfully</div><h2>{application.applicationNumber}</h2><p>Your application was submitted on {formatDate(application.submittedDate)}. SMS confirmation is sent to the registered mobile number when delivery is enabled.</p></section>}
      <div className={styles.eyebrow}>Application details</div><h1>{application.applicationNumber}</h1><p className={styles.lede}><span className={styles.status}>{application.status}</span> &nbsp; Created {formatDate(application.createdDate)}</p>
      <section className={styles.card}><h2>Applicant and property information</h2><dl className={styles.detailGrid}>{Object.entries({ "Application type": application.applicationTypeName, "Applicant type": application.applicantTypeName, "Full name": application.fullName, Address: application.address, "Email ID": application.emailId, "Mobile number": application.mobileNo, "Aadhar number": application.aadharNo, "Peth name": application.petName, "Peth number": application.pethNo, Zone: application.zoneNo, Prabhag: application.prabhagNo, "Property tax number": application.propertyTaxNo }).map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value}</dd></div>)}</dl></section>
      <section className={styles.card} style={{ marginTop: 18 }}><h2>Tree information</h2><dl className={styles.detailGrid}>{Object.entries({ "Tree address": application.treeAddress, "Tree cutting reason": application.treeCuttingReason, "Number of trees": application.numberOfTreeCutting, "Tree species": application.treeSpecies }).map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{String(value)}</dd></div>)}</dl></section>
      <section className={styles.card} style={{ marginTop: 18 }}><h2>Documents</h2>{application.documents.length === 0 ? <p className={styles.lede}>No documents uploaded.</p> : <div className={styles.uploadList}>{application.documents.map((document) => <div className={styles.uploadRow} key={document.applicationDocumentId}><span>{document.documentTypeName}: {document.fileName}</span><a className={styles.cardLink} href={citizenFileUrl(`/api/citizen/applications/${application.applicationId}/documents/${document.applicationDocumentId}/file`)} target="_blank" rel="noreferrer">View</a></div>)}</div>}</section>
      <section className={styles.card} style={{ marginTop: 18 }}><h2>Tree photographs</h2><div className={styles.photoGrid}>{application.photos.map((photo) => <a key={photo.applicationPhotoId} href={citizenFileUrl(`/api/citizen/applications/${application.applicationId}/photos/${photo.applicationPhotoId}/file`)} target="_blank" rel="noreferrer"><article className={styles.photoCard}><img src={citizenFileUrl(`/api/citizen/applications/${application.applicationId}/photos/${photo.applicationPhotoId}/file`)} alt={photo.fileName} /><div>{photo.fileName}</div></article></a>)}</div></section>
      <div className={styles.actions}><Link className={styles.buttonSecondary} href="/citizen/dashboard">Go to dashboard</Link></div>
    </>
  );
}
