const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5216";
const TOKEN_STORAGE_KEY = process.env.NEXT_PUBLIC_SMC_TOKEN_STORAGE_KEY ?? "smc_access_token";

export async function citizenFetch(path: string, options: RequestInit = {}) {
  const headers = new Headers(options.headers);
  if (typeof window !== "undefined") {
    const token = window.sessionStorage.getItem(TOKEN_STORAGE_KEY);
    if (token) headers.set("Authorization", `Bearer ${token}`);
  }
  if (options.body && !(options.body instanceof FormData)) headers.set("Content-Type", "application/json");
  return fetch(`${API_BASE_URL}${path}`, { ...options, headers, credentials: "include" });
}

export function citizenFileUrl(path: string) {
  return `${API_BASE_URL}${path}`;
}

export async function responseMessage(response: Response, fallback: string) {
  const text = await response.text();
  try {
    const body = JSON.parse(text) as { message?: string };
    return body.message ?? fallback;
  } catch {
    return text || fallback;
  }
}

export type MasterOption = { id: number; name: string };
export type DocumentRequirement = { documentTypeId: number; documentTypeName: string; isRequired: boolean; displayOrder: number };
export type CitizenProfile = { citizenId: string; name: string; mobileNumber: string; email: string; address: string };
export type CitizenApplicationSummary = {
  applicationId: number;
  applicationNumber: string;
  applicationTypeName: string;
  status: string;
  createdDate: string;
  updatedDate: string | null;
  submittedDate: string | null;
};
export type ApplicationPhoto = { applicationPhotoId: number; applicationId: number; fileName: string; filePath: string; contentType: string; uploadedDate: string };
export type ApplicationDocument = { applicationDocumentId: number; applicationId: number; applicationTypeId: number; documentTypeId: number; documentTypeName: string; fileName: string; filePath: string; contentType: string; uploadedDate: string };
export type ApplicationDetail = {
  applicationId: number;
  applicationNumber: string;
  applicationTypeId: number;
  applicationTypeName: string;
  applicantTypeId: number;
  applicantTypeName: string;
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
  numberOfTreeCutting: number;
  treeSpecies: string;
  createdDate: string;
  updatedDate: string | null;
  submittedDate: string | null;
  isSubmitted: boolean;
  status: string;
  documents: ApplicationDocument[];
  photos: ApplicationPhoto[];
};

export const formatDate = (value: string | null) => value ? new Date(value).toLocaleString() : "-";
