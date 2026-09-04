import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Tree Cutting Application Form",
  description: "Tree cutting application and document upload form",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
