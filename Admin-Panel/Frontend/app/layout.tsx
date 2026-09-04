import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = { title: "SMC Tree Cutting ERP", description: "Solapur Municipal Corporation tree cutting administration" };

export default function AdminLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
