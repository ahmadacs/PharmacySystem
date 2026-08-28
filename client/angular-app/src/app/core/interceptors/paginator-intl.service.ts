import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { TranslateService } from '@ngx-translate/core';

@Injectable()
export class CustomPaginatorIntl extends MatPaginatorIntl {
  constructor(private translate: TranslateService) {
    super();

    this.translate.onLangChange.subscribe(() => {
      this.updateLabels();
    });

    this.updateLabels();
  }

  // In Angular 22, getRangeLabel is a property that holds a function
  // We override it with a getter that returns the function
  override getRangeLabel = (page: number, pageSize: number, length: number): string => {
    if (length === 0) {
      return this.translate.instant('common.paginator.pageInfo', { 1: 0, 2: 0, 3: 0 });
    }
    const start = page * pageSize + 1;
    const end = Math.min((page + 1) * pageSize, length);
    return this.translate.instant('common.paginator.pageInfo', { 1: start, 2: end, 3: length });
  };

  private updateLabels() {
    this.itemsPerPageLabel = this.translate.instant('common.paginator.itemsPerPage');
    this.nextPageLabel = this.translate.instant('common.paginator.nextPage');
    this.previousPageLabel = this.translate.instant('common.paginator.previousPage');
    this.firstPageLabel = this.translate.instant('common.paginator.firstPage');
    this.lastPageLabel = this.translate.instant('common.paginator.lastPage');
  }
}