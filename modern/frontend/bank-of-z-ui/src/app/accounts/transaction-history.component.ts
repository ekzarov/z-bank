import { CurrencyPipe, DatePipe, TitleCasePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountApiService } from './account-api.service';
import { TransactionHistoryItem } from './account.model';

@Component({
  selector: 'app-transaction-history',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink, TitleCasePipe],
  templateUrl: './transaction-history.component.html',
  styleUrl: './transaction-history.component.scss'
})
export class TransactionHistoryComponent {
  private readonly api = inject(AccountApiService);
  private readonly route = inject(ActivatedRoute);
  protected readonly accountId = signal('');
  protected readonly items = signal<TransactionHistoryItem[]>([]);
  protected readonly nextCursor = signal<string | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly filters = new FormGroup({
    from: new FormControl('', { nonNullable: true }),
    to: new FormControl('', { nonNullable: true })
  });

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe(parameters => {
      this.accountId.set(parameters.get('id') ?? '');
      this.load(false);
    });
  }

  protected applyFilters(): void {
    this.load(false);
  }

  protected loadMore(): void {
    this.load(true);
  }

  private load(append: boolean): void {
    const accountId = this.accountId();
    if (!/^\d{8}$/.test(accountId) || this.loading()) {
      if (!/^\d{8}$/.test(accountId)) this.error.set('The account link is invalid.');
      return;
    }

    const { from, to } = this.filters.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.api.history(
      accountId,
      from ? `${from}T00:00:00Z` : undefined,
      to ? `${to}T00:00:00Z` : undefined,
      append ? this.nextCursor() ?? undefined : undefined)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: page => {
          this.items.update(current => append ? [...current, ...page.items] : page.items);
          this.nextCursor.set(page.nextCursor);
        },
        error: response => {
          if (!append) this.items.set([]);
          this.error.set(response?.error?.code === 'invalid_history_range'
            ? 'Choose a To date later than the From date.'
            : response.status === 404
              ? 'Transaction history was not found.'
              : 'Transaction history is currently unavailable.');
        }
      });
  }
}
