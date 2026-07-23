import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { StatementApiService } from './statement-api.service';
import { BulkStatementResult } from './statement.model';

@Component({
  selector: 'app-bulk-statements',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './bulk-statements.component.html',
  styleUrl: './bulk-statements.component.scss'
})
export class BulkStatementsComponent {
  private readonly api = inject(StatementApiService);
  protected readonly result = signal<BulkStatementResult | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal('');
  protected readonly form = new FormGroup({
    year: new FormControl(previousMonth().year, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(2000), Validators.max(2100)]
    }),
    month: new FormControl(previousMonth().month, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(12)]
    })
  });

  protected generate(): void {
    this.submit();
  }

  protected retryFailed(): void {
    const accountIds = this.result()?.accounts
      .filter(account => !account.succeeded)
      .map(account => account.accountId) ?? [];
    if (accountIds.length) {
      this.submit(accountIds);
    }
  }

  private submit(accountIds?: string[]): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    const { year, month } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.api.generateBulk(year, month, accountIds)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: result => this.mergeResult(result, accountIds),
        error: () => this.error.set('The bulk statement run could not be completed.')
      });
  }

  private mergeResult(next: BulkStatementResult, retriedIds?: string[]): void {
    const current = this.result();
    if (!current || !retriedIds?.length) {
      this.result.set(next);
      return;
    }

    const replacements = new Map(next.accounts.map(account => [account.accountId, account]));
    const accounts = current.accounts.map(account => replacements.get(account.accountId) ?? account);
    this.result.set({
      ...next,
      total: accounts.length,
      succeeded: accounts.filter(account => account.succeeded).length,
      failed: accounts.filter(account => !account.succeeded).length,
      accounts
    });
  }
}

function previousMonth(now = new Date()): { year: number; month: number } {
  const date = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1));
  return { year: date.getUTCFullYear(), month: date.getUTCMonth() + 1 };
}
