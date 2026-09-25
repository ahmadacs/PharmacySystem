import { HttpParams, httpResource } from '@angular/common/http';
import { DestroyRef, Signal, WritableSignal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { PageEvent } from '@angular/material/paginator';
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

export type PagedExtraParams = Record<string, string | number | boolean | null | undefined>;

/**
 * Builds the base paged-table params (page, pageSize, search, sortBy, sortDir)
 * plus any caller-specific extra params. Entries whose value is null/undefined
 * are skipped, so optional filters can be passed directly:
 * `buildPagedParams(this.table, { status: this.status() })`.
 */
export function buildPagedParams(table: PagedTable, extra: PagedExtraParams = {}): HttpParams {
  let params = new HttpParams()
    .set('page', table.page())
    .set('pageSize', table.pageSize())
    .set('search', table.search())
    .set('sortBy', table.sortBy())
    .set('sortDir', table.sortDir());
  for (const [key, value] of Object.entries(extra)) {
    if (value !== null && value !== undefined) {
      params = params.set(key, value);
    }
  }
  return params;
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
 * Refreshes a server-side paged table: reloads in place when already on the
 * first page, otherwise jumps back to it (which triggers a reload).
 * Replaces the 4x identical private `refreshX()` methods in the list screens.
 */
export function refreshPaged(table: PagedTable, resource: { reload(): unknown }): void {
  if (table.page() === 1) {
    resource.reload();
  } else {
    table.page.set(1);
  }
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

export interface PagedTab<TData, TFilter> {
  table: PagedTable;
  data: TData;
  count: Signal<number>;
  filter: WritableSignal<TFilter>;
}

export interface PagedTabWithoutFilter<TData> {
  table: PagedTable;
  data: TData;
  count: Signal<number>;
  filter?: undefined;
}

/**
 * Generic bundle for one tab of a multi-tab screen: its table state, its
 * paged resource, the resource's totalCount, and its optional filter signal.
 * Generics preserve each tab's exact DTO/filter types, so template bindings
 * (e.g. `tabs.batches.filter.set(...)`) keep full type-checking.
 */
export function createPagedTab<TData extends { totalCount: Signal<number> }, TFilter>(
  table: PagedTable,
  data: TData,
  filter: WritableSignal<TFilter>,
): PagedTab<TData, TFilter>;
export function createPagedTab<TData extends { totalCount: Signal<number> }>(
  table: PagedTable,
  data: TData,
): PagedTabWithoutFilter<TData>;
export function createPagedTab(
  table: PagedTable,
  data: { totalCount: Signal<number> },
  filter?: WritableSignal<unknown>,
): {
  table: PagedTable;
  data: { totalCount: Signal<number> };
  count: Signal<number>;
  filter?: WritableSignal<unknown>;
} {
  return { table, data, count: data.totalCount, filter };
}
