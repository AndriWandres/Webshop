import { Component, OnDestroy, OnInit } from '@angular/core';
import { select, Store } from '@ngrx/store';
import { Subject } from 'rxjs';
import { startWith, takeUntil } from 'rxjs/operators';
import { AppStoreState } from 'src/app/app-store';
import { ProductStoreActions, ProductStoreSelectors } from 'src/app/app-store/products-store';
import { ProductListing } from 'src/models/products/productListing';

@Component({
  selector: 'app-products',
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.scss']
})
export class ProductsComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();

  readonly products$ = this.store$.pipe(
    select(ProductStoreSelectors.selectAll),
    takeUntil(this.destroy$),
  );

  constructor(private readonly store$: Store<AppStoreState.State>) { }

  ngOnInit(): void {
    this.store$.dispatch(ProductStoreActions.getProducts());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
