import { CurrencyPipe, TitleCasePipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountApiService } from './account-api.service';
import { Account, AccountMetadata, AccountType } from './account.model';

@Component({
  selector: 'app-account-portfolio',
  imports: [CurrencyPipe, ReactiveFormsModule, RouterLink, TitleCasePipe],
  templateUrl: './account-portfolio.component.html',
  styleUrl: './account-portfolio.component.scss'
})
export class AccountPortfolioComponent {
  private readonly api = inject(AccountApiService);
  readonly customerId = input.required<string>();
  readonly operator = input(false);
  protected readonly accounts = signal<Account[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));
  protected readonly loading = signal(false);
  protected readonly creating = signal(false);
  protected readonly error = signal('');
  protected readonly message = signal('');
  protected readonly form = new FormGroup({
    type: new FormControl<AccountType>('current', { nonNullable: true, validators: Validators.required }),
    interestRate: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0), Validators.max(9999.99)] }),
    overdraftLimit: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    currency: new FormControl('GBP', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)] })
  });

  constructor() {
    effect(() => {
      const customerId = this.customerId();
      this.page.set(1);
      this.load(customerId, 1);
    });
  }

  protected startCreate(): void {
    this.creating.set(true);
    this.error.set('');
    this.message.set('');
    this.form.reset({ type: 'current', interestRate: 0, overdraftLimit: 0, currency: 'GBP' });
  }

  protected cancelCreate(): void {
    this.creating.set(false);
  }

  protected create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.error.set('');
    this.api.create(this.customerId(), this.form.getRawValue() as AccountMetadata)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: account => {
          this.creating.set(false);
          this.message.set(`Account ${account.id} created.`);
          const targetPage = Math.ceil((this.total() + 1) / this.pageSize);
          this.page.set(targetPage);
          this.load(this.customerId(), targetPage);
        },
        error: response => this.error.set(response.status === 409
          ? 'This customer already has the maximum of ten active accounts.'
          : 'Account could not be created. Check the product terms.')
      });
  }

  protected previousPage(): void {
    if (this.page() <= 1) return;
    this.page.update(value => value - 1);
    this.load(this.customerId(), this.page());
  }

  protected nextPage(): void {
    if (this.page() * this.pageSize >= this.total()) return;
    this.page.update(value => value + 1);
    this.load(this.customerId(), this.page());
  }

  private load(customerId: string, page: number): void {
    this.loading.set(true);
    this.error.set('');
    this.api.list(customerId, page, this.pageSize).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: page => {
        this.accounts.set(page.items);
        this.total.set(page.total);
      },
      error: () => this.error.set('Accounts are currently unavailable.')
    });
  }
}
