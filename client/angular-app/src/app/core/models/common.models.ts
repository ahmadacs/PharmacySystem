export interface ErrorEnvelope {
  success: boolean;
  message: string | null;
  errors: Record<string, string[]> | null;
  traceId: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
