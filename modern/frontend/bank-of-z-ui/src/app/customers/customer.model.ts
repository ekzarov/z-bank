export interface CustomerDetails {
  title: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  region: string | null;
  postalCode: string;
  countryCode: string;
  email: string;
  phone: string | null;
}

export interface Customer {
  id: string;
  sortCode: string;
  details: CustomerDetails;
  status: 'active' | 'retired';
  creditScore: number;
  creditReviewDate: string;
  sourceSystem: 'cics' | 'ims' | 'modern';
  sourceIdentifier: string | null;
  createdAt: string;
  updatedAt: string;
  version: string;
}

export interface CreateCustomerRequest {
  details: CustomerDetails;
  sourceSystem: 'modern';
  sourceIdentifier: string | null;
}
