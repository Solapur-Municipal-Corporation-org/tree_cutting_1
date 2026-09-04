import Link from "next/link";
import styles from "./citizen.module.css";

export default function CitizenLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className={styles.shell}>
      <header className={styles.topbar}>
        <div className={styles.brand}>SMC Tree Care<span>Citizen services</span></div>
        <nav className={styles.nav} aria-label="Citizen navigation">
          <Link href="/citizen/dashboard">Dashboard</Link>
          <Link href="/citizen/tree-cutting/apply">New application</Link>
        </nav>
      </header>
      <main className={styles.content}>{children}</main>
    </div>
  );
}
