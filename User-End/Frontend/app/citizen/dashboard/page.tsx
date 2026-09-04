"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import styles from "../citizen.module.css";
import { citizenFetch, CitizenApplicationSummary, CitizenProfile, formatDate, responseMessage } from "../citizen-api";

export default function CitizenDashboardPage() {
  const [profile, setProfile] = useState<CitizenProfile | null>(null);
  const [applications, setApplications] = useState<CitizenApplicationSummary[]>([]);
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([citizenFetch("/api/citizen/profile"), citizenFetch("/api/citizen/applications")])
      .then(async ([profileResponse, applicationsResponse]) => {
        if (!profileResponse.ok || !applicationsResponse.ok) {
          throw new Error(await responseMessage(profileResponse.ok ? applicationsResponse : profileResponse, "Your SMC session has expired. Please return through the common SMC login."));
        }
        setProfile(await profileResponse.json());
        setApplications(await applicationsResponse.json());
      })
      .catch((error: unknown) => setMessage(error instanceof Error ? error.message : "Unable to load your dashboard."))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p className={styles.loading}>Loading your citizen dashboard...</p>;
  if (message) return <div className={styles.alert}>{message}</div>;

  return (
    <>
      <div className={styles.eyebrow}>Citizen dashboard</div>
      <h1>Welcome{profile?.name ? `, ${profile.name}` : ""}</h1>
      <p className={styles.lede}>Apply for tree cutting permission and track your applications with Solapur Municipal Corporation.</p>
      <div className={styles.grid}>
        <section className={styles.card}>
          <h3>Tree cutting application</h3>
          <p>Submit property and tree details, required documents, and photographs for review.</p>
          <Link className={styles.cardLink} href="/citizen/tree-cutting/apply">Start a new application</Link>
        </section>
        <section className={styles.card}>
          <h3>My applications</h3>
          <p>{applications.length} application{applications.length === 1 ? "" : "s"} associated with your citizen account.</p>
          <a className={styles.cardLink} href="#my-applications">View applications</a>
        </section>
        <section className={styles.card}>
          <h3>Your profile</h3>
          <p>{profile?.mobileNumber || profile?.email || "Profile details are supplied by the common SMC login."}</p>
        </section>
      </div>
      <section id="my-applications" className={styles.card} style={{ marginTop: 28 }}>
        <h2>My applications</h2>
        {applications.length === 0 ? <p className={styles.lede}>You have not submitted a tree cutting application yet.</p> : (
          <div className={styles.tableWrap}>
            <table>
              <thead><tr><th>Application number</th><th>Date</th><th>Type</th><th>Status</th><th /></tr></thead>
              <tbody>{applications.map((application) => (
                <tr key={application.applicationId}>
                  <td>{application.applicationNumber}</td>
                  <td>{formatDate(application.submittedDate ?? application.createdDate)}</td>
                  <td>{application.applicationTypeName}</td>
                  <td><span className={styles.status}>{application.status}</span></td>
                  <td><Link className={styles.cardLink} href={`/citizen/tree-cutting/application/${application.applicationId}`}>View details</Link></td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
