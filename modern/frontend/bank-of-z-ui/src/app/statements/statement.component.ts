import { CurrencyPipe, DatePipe, TitleCasePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { StatementApiService } from './statement-api.service';
import { StatementView } from './statement.model';

@Component({
  selector: 'app-statement',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink, TitleCasePipe],
  templateUrl: './statement.component.html',
  styleUrl: './statement.component.scss'
})
export class StatementComponent {
  private readonly api = inject(StatementApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly accountId = signal('');
  protected readonly statement = signal<StatementView | null>(null);
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

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe(parameters => {
      const accountId = parameters.get('id') ?? '';
      const generationId = parameters.get('generationId');
      this.accountId.set(accountId);
      this.statement.set(null);
      this.error.set('');
      if (!/^\d{8}$/.test(accountId)) {
        this.error.set('The account link is invalid.');
      } else if (generationId) {
        this.load(accountId, generationId);
      }
    });
  }

  protected generate(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    const { year, month } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set('');
    this.api.generate(this.accountId(), year, month)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: statement => {
          this.statement.set(statement);
          void this.router.navigate(
            ['/accounts', statement.accountId, 'statements', statement.generationId],
            { replaceUrl: true });
        },
        error: response => this.error.set(this.errorMessage(response?.error?.code))
      });
  }

  protected print(): void {
    window.print();
  }

  private load(accountId: string, generationId: string): void {
    this.loading.set(true);
    this.api.find(accountId, generationId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: statement => {
          this.statement.set(statement);
          this.form.setValue({ year: statement.year, month: statement.month });
        },
        error: response => this.error.set(response.status === 404
          ? 'The statement was not found.'
          : 'The statement is currently unavailable.')
      });
  }

  private errorMessage(code?: string): string {
    switch (code) {
      case 'invalid_statement_period': return 'Choose a valid calendar month.';
      case 'statement_account_not_found': return 'The account was not found.';
      case 'statement_generation_failed': return 'The statement could not be reconciled and was not published.';
      default: return 'The statement could not be generated.';
    }
  }
}

function previousMonth(now = new Date()): { year: number; month: number } {
  const date = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1));
  return { year: date.getUTCFullYear(), month: date.getUTCMonth() + 1 };
}
