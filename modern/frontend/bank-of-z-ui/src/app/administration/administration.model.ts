export type AdministrationRole = 'Customer' | 'Operator' | 'Administrator';

export interface AdministrationUser {
  id: string;
  userName: string;
  email: string;
  role: AdministrationRole;
  customerId: string | null;
  isLockedOut: boolean;
  lockoutEnd: string | null;
  accessFailedCount: number;
  version: string;
}

export interface AdministrationUserPage {
  items: AdministrationUser[];
  page: number;
  pageSize: number;
  total: number;
}

export interface CreateAdministrationUserRequest {
  userName: string;
  email: string;
  password: string;
  role: AdministrationRole;
  customerId: string | null;
}

export interface SecurityAuditEntry {
  id: number;
  occurredAt: string;
  eventName: string;
  actorId: string | null;
  subjectId: string | null;
  succeeded: boolean;
  outcome: string;
  correlationId: string;
}

export interface SecurityAuditPage {
  items: SecurityAuditEntry[];
  page: number;
  pageSize: number;
  total: number;
}

export interface SecurityAuditFilters {
  eventName: string;
  actorOrSubject: string;
  succeeded: '' | 'true' | 'false';
  from: string;
  to: string;
}
