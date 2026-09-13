import { HttpParams, httpResource } from '@angular/common/http';
import { DestroyRef, WritableSignal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { Sort } from '@angular/material/sort';
import { debounceTime, map } from 'rxjs';
import { PagedResult } from '../models/api.models';
import { emptyPage } from './empty-page';

export interface PagedRequest {
  url: string;
  params: HttpParams;
}

export interface PagedTableOptions {
  defaultSortBy: string;
  defaultSortDir?: string;
  defaultPageSize?: number;
  /** Debounce for the search box (ms). */
  searchDebounceMs?: number;
}

/**
 * Generic server-side paged table state.
 *
 * Replaces the 4x copy-pasted `onXxxSort/onXxxPage + debounce effect` blocks
 * in InventoryComponent and the duplicates in MedicinesListComponent.
 *
 * Usage:
 * ```ts
 * readonly table = createPagedTable({ defaultSortBy: 'name' });
 * readonly data = createPagedResource(() => ({
 *   url: `${environment.apiUrl}/medicines`,
 *   params: new HttpParams()
 *     .set('page', this.table.page())
 *     .set('pageSize', this.table.pageSize())
 *     .set('search', this.table.search())
 *     ...
 * }));
 * ```
 */
export interface PagedTable {
  page: WritableSignal<number>;
  pageSize: WritableSignal<number>;
  search: WritableSignal<string>;
  sortBy: WritableSignal<string>;
  sortDir: WritableSignal<string>;
  searchControl: FormControl<string | null>;
  onSortChange(sort: Sort): void;
  onPage(event: PageEvent): void;
  resetToFirstPage(): void;
}

export function createPagedTable(options: PagedTableOptions, destroyRef?: DestroyRef): PagedTable {
  const page = signal(1);
  const pageSize = signal(options.defaultPageSize ?? 10);
  const search = signal('');
  const sortBy = signal(options.defaultSortBy);
  const sortDir = signal(options.defaultSortDir ?? 'asc');
  const searchControl = new FormControl<string | null>('');

  const ref = destroyRef ?? inject(DestroyRef);
  searchControl.valueChanges
    .pipe(
      map((value) => (value ?? '').trim()),
      debounceTime(options.searchDebounceMs ?? 300),
      takeUntilDestroyed(ref),
    )
    .subscribe((value) => {
      search.set(value);
      page.set(1);
    });

  return {
    page,
    pageSize,
    search,
    sortBy,
    sortDir,
    searchControl,
    onSortChange(sort: Sort): void {
      if (!sort.active) {
        return;
      }
      sortBy.set(sort.active);
      sortDir.set(sort.direction === 'asc' ? 'asc' : 'desc');
      page.set(1);
    },
    onPage(event: PageEvent): void {
      pageSize.set(event.pageSize);
      page.set(event.pageIndex + 1);
    },
    resetToFirstPage(): void {
      page.set(1);
    },
  };
}

/**
 * Thin wrapper around `httpResource` with a typed empty-page default value.
 */
export function createPagedResource<T>(request: () => PagedRequest | undefined) {
  const resource = httpResource<PagedResult<T>>(request, {
    defaultValue: emptyPage<T>(),
  });
  const totalCount = computed(() => resource.value()?.totalCount ?? 0);
  return Object.assign(resource, { totalCount });
}

export { MatPaginator };
export type { PageEvent as PagedTablePageEvent };
