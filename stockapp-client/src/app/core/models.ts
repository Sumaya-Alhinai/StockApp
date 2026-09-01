export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  token: string;
}

export interface RegisterResult {
  id: string;
  fullName: string;
  email: string;
}

export interface Product {
  id: string;
  name: string;
  sku: string;
  price: number;
  category: string | null;
  isActive: boolean;
  stockOnHand: number;
  rowVersion: string;
  createdAt: string;
}

export enum MovementType {
  In = 1,
  Out = 2
}

export interface StockMovement {
  id: string;
  movementType: MovementType;
  quantity: number;
  note: string | null;
  createdAt: string;
}

/**
 * Mirrors PagedResult<T> returned by the API's list endpoints.
 * Field names must match the JSON exactly (camelCase), otherwise
 * the values arrive as undefined with no compiler warning.
 */
export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiError {
  code: string;
  message: string;
  errors: Record<string, string[]> | null;
}