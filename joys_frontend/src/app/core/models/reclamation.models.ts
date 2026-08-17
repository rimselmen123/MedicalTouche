export type ReclamationStatus = 'New' | 'InProgress' | 'Resolved';

export interface Reclamation {
  id: number;
  fullName: string;
  email: string;
  phone?: string;
  subject?: string;
  message?: string;
  status: ReclamationStatus;
  orderId?: number;
  adminNote?: string;
  createdAt: string;
}

export interface CreateReclamationRequest {
  fullName: string;
  email: string;
  phone?: string;
  subject?: string;
  message: string;
  orderId?: number;
}

export interface RespondReclamationRequest {
  status: ReclamationStatus;
  adminNote?: string;
}
