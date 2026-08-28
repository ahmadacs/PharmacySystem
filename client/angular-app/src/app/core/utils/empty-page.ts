import { PagedResult } from '../models/api.models';

export const emptyPage = <T>(): PagedResult<T> => ({
  items: [],
  page: 1,
  pageSize: 10,
  totalCount: 0
});