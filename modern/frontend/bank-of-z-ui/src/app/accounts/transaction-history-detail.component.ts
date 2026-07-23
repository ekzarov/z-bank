import { CurrencyPipe, DatePipe, TitleCasePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AccountApiService } from './account-api.service';
import { TransactionHistoryItem } from './account.model';

@Component({
  selector: 'app-transaction-history-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, TitleCasePipe],
  templateUrl: './transaction-history-detail.component.html',
  styleUrl: './transaction-history-detail.component.scss'
})
export class TransactionHistoryDetailComponent {
  private readonly api = inject(AccountApiService);
  private readonly route = inject(ActivatedRoute);
  protected readonly accountId = signal('');
  protected readonly transaction = signal<TransactionHistoryItem | null>(null);
  protected readonly error = signal('');

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe(parameters => {
      const accountId = parameters.get('id') ?? '';
      const reference = parameters.get('reference') ?? '';
      this.accountId.set(accountId);
      this.load(accountId, reference);
    });
  }

  private load(accountId: string, reference: string): void {
    if (!/^\d{8}$/.test(accountId) || !reference) {
      this.error.set('The transaction link is invalid.');
      return;
    }
    this.api.transaction(accountId, reference).subscribe({
      next: transaction => { this.transaction.set(transaction); this.error.set(''); },
      error: response => this.error.set(response.status === 404
        ? 'Transaction was not found.'
        : 'Transaction details are currently unavailable.')
    });
  }
}
