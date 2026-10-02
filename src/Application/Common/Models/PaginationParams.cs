namespace Application.Common.Models;

public readonly record struct PaginationParams(int Page, int PageSize, bool Descending);
