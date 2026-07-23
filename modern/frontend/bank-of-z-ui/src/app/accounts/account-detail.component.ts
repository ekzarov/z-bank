import { CurrencyPipe, TitleCasePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { SessionService } from '../core/session.service';
import { AccountApiService } from './account-api.service';
import { Account, AccountMetadata, AccountType, CashTransactionDirection } from './account.model';

@Component({
  selector: 'app-account-detail',
  imports: [CurrencyPipe, ReactiveFormsModule, RouterLink, TitleCasePipe],
  templateUrl: './account-detail.component.html',
  styleUrl: './account-detail.component.scss'
})
export class AccountDetailComponent {
  private readonly api = inject(AccountApiService);
  private readonly route = inject(ActivatedRoute);
  protected readonly session = inject(SessionService);
  protected readonly account = signal<Account | null>(null);
  protected readonly editing = signal(false);
  protected readonly saving = signal(false);
  protected readonly booking = signal(false);
  protected readonly transferring = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly form = new FormGroup({
    type: new FormControl<AccountType>('current', { nonNullable: true, validators: Validators.required }),
    interestRate: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(9999.99)] }),
    overdraftLimit: new FormControl(0, { nonNullable: true, validators: Validators.min(0) }),
    currency: new FormControl('GBP', { nonNullable: true, validators: Validators.pattern(/^[A-Za-z]{3}$/) })
  });
  protected readonly cashForm = new FormGroup({
    direction: new FormControl<CashTransactionDirection>('deposit', { nonNullable: true, validators: Validators.required }),
    amount: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(0.01), Validators.max(9999999999999999.99)] })
  });
  protected readonly transferForm = new FormGroup({
    destinationAccountId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^\d{8}$/)]
    }),
    amount: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(0.01), Validators.max(9999999999999999.99)]
    })
  });

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe(parameters =>
      this.load(parameters.get('id') ?? ''));
  }

  protected isOperator(): boolean { return this.session.session()?.roles.includes('Operator') ?? false; }

  protected startEdit(): void {
    const account = this.account();
    if (!account) return;
    this.form.reset({ type: account.type, interestRate: account.interestRate, overdraftLimit: account.overdraftLimit, currency: account.currency });
    this.editing.set(true);
  }

  protected save(): void {
    const account = this.account();
    if (!account || this.form.invalid) return;
    this.saving.set(true);
    this.api.update(account.id, this.form.getRawValue() as AccountMetadata, account.version)
      .pipe(finalize(() => this.saving.set(false))).subscribe({
        next: value => { this.account.set(value); this.editing.set(false); this.message.set('Account terms updated.'); },
        error: response => this.error.set(response.status === 409 ? 'This account changed. Reload before saving.' : 'Account terms could not be updated.')
      });
  }

  protected close(): void {
    const account = this.account();
    if (!account || !confirm(`Close account ${account.id}?`)) return;
    this.saving.set(true);
    this.api.close(account.id, account.version).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => this.load(account.id, 'Account closed.'),
      error: () => this.error.set('Only a zero-balance account without pending work can be closed.')
    });
  }

  protected bookCash(): void {
    const account = this.account();
    if (!account || this.cashForm.invalid || this.booking()) {
      this.cashForm.markAllAsTouched();
      return;
    }

    const { direction, amount } = this.cashForm.getRawValue();
    this.booking.set(true);
    this.error.set('');
    this.message.set('');
    this.api.bookCash(account.id, direction, amount!)
      .pipe(finalize(() => this.booking.set(false)))
      .subscribe({
        next: result => {
          this.cashForm.controls.amount.reset();
          const operation = direction === 'deposit' ? 'Deposit' : 'Withdrawal';
          this.load(account.id, `${operation} ${result.reference} booked. Available balance: ${this.formatMoney(result.availableBalance, result.currency)}.`);
        },
        error: response => this.error.set(this.cashError(response?.error?.code))
      });
  }

  protected transfer(): void {
    const account = this.account();
    if (!account || this.transferForm.invalid || this.transferring()) {
      this.transferForm.markAllAsTouched();
      return;
    }

    const { destinationAccountId, amount } = this.transferForm.getRawValue();
    this.transferring.set(true);
    this.error.set('');
    this.message.set('');
    this.api.transfer(account.id, destinationAccountId, amount!)
      .pipe(finalize(() => this.transferring.set(false)))
      .subscribe({
        next: result => {
          this.transferForm.reset();
          this.load(
            account.id,
            `Transfer ${result.correlationId} booked. Available balance: ${this.formatMoney(result.source.availableBalance, result.source.currency)}.`);
        },
        error: response => this.error.set(this.transferError(response?.error?.code))
      });
  }

  private cashError(code?: string): string {
    switch (code) {
      case 'insufficient_funds': return 'The withdrawal exceeds the available balance and overdraft limit.';
      case 'cash_account_inactive': return 'Cash operations are unavailable for a closed account.';
      case 'cash_product_not_supported': return 'Cash operations are unavailable for this account product.';
      case 'cash_amount_invalid': return 'Enter a positive amount with no more than two decimal places.';
      case 'idempotency_conflict': return 'This operation conflicts with an earlier request. Please try again.';
      default: return 'The cash operation could not be completed.';
    }
  }

  private transferError(code?: string): string {
    switch (code) {
      case 'transfer_same_account': return 'Choose a different destination account.';
      case 'transfer_currency_mismatch': return 'Source and destination accounts must use the same currency.';
      case 'transfer_account_not_found': return 'The destination account was not found or is unavailable.';
      case 'insufficient_funds': return 'The transfer exceeds the available balance and overdraft limit.';
      case 'cash_account_inactive': return 'Transfers require two active accounts.';
      case 'cash_product_not_supported': return 'Transfers are unavailable for this account product.';
      case 'cash_amount_invalid': return 'Enter a positive amount with no more than two decimal places.';
      case 'idempotency_conflict': return 'This transfer conflicts with an earlier request. Please try again.';
      default: return 'The transfer could not be completed.';
    }
  }

  private formatMoney(amount: number, currency: string): string {
    return new Intl.NumberFormat('en-GB', { style: 'currency', currency }).format(amount);
  }

  private load(id: string, message = ''): void {
    if (!/^\d{8}$/.test(id)) { this.error.set('The account link is invalid.'); return; }
    this.api.find(id).subscribe({
      next: account => { this.account.set(account); this.error.set(''); this.message.set(message); },
      error: response => this.error.set(response.status === 404 ? 'Account was not found.' : 'Account is currently unavailable.')
    });
  }
}
