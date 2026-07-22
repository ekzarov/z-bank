import { CurrencyPipe, TitleCasePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { SessionService } from '../core/session.service';
import { AccountApiService } from './account-api.service';
import { Account, AccountMetadata, AccountType } from './account.model';

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
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly form = new FormGroup({
    type: new FormControl<AccountType>('current', { nonNullable: true, validators: Validators.required }),
    interestRate: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(9999.99)] }),
    overdraftLimit: new FormControl(0, { nonNullable: true, validators: Validators.min(0) }),
    currency: new FormControl('GBP', { nonNullable: true, validators: Validators.pattern(/^[A-Za-z]{3}$/) })
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

  private load(id: string, message = ''): void {
    if (!/^\d{8}$/.test(id)) { this.error.set('The account link is invalid.'); return; }
    this.api.find(id).subscribe({
      next: account => { this.account.set(account); this.error.set(''); this.message.set(message); },
      error: response => this.error.set(response.status === 404 ? 'Account was not found.' : 'Account is currently unavailable.')
    });
  }
}
