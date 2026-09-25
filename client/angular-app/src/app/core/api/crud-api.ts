import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

/** Shared GET-by-id and POST endpoints for entity services. */
export abstract class ResourceApi<TDetail, TCreate> {
  protected constructor(
    protected readonly http: HttpClient,
    protected readonly baseUrl: string,
  ) {}

  get(id: string): Promise<TDetail> {
    return firstValueFrom(this.http.get<TDetail>(`${this.baseUrl}/${id}`));
  }

  create(request: TCreate): Promise<TDetail> {
    return firstValueFrom(this.http.post<TDetail>(this.baseUrl, request));
  }
}

/** Adds PATCH and DELETE for resources that support full CRUD. */
export abstract class CrudApi<TDetail, TCreate, TUpdate> extends ResourceApi<TDetail, TCreate> {
  protected constructor(http: HttpClient, baseUrl: string) {
    super(http, baseUrl);
  }

  update(id: string, request: TUpdate): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`${this.baseUrl}/${id}`, request));
  }

  remove(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${id}`));
  }
}
