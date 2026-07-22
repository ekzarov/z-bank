import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { finalize, Observable } from 'rxjs';
import { CustomerApiService } from './customer-api.service';
import { createCustomerForm, formToDetails } from './customer-form';
import { Customer } from './customer.model';

@Component({
  selector: 'app-customer-workspace',
  imports: [ReactiveFormsModule],
  templateUrl: './customer-workspace.component.html',
  styleUrl: './customer-workspace.component.scss'
})
export class CustomerWorkspaceComponent {
  private readonly api = inject(CustomerApiService);
  protected readonly query = signal('');
  protected readonly results = signal<Customer[]>([]);
  protected readonly selected = signal<Customer | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly creating = signal(false);
  protected readonly message = signal('');
  protected readonly error = signal('');
  protected readonly form = createCustomerForm();

  protected search(): void {
    const query = this.query().trim();
    if (!query) return;
    this.loading.set(true);
    this.error.set('');
    const request: Observable<Customer | Customer[]> = /^\d{10}$/.test(query)
      ? this.api.find(query)
      : this.api.search(query);
    request.pipe(finalize(() => this.loading.set(false))).subscribe({
      next: value => {
        const customers = Array.isArray(value) ? value : [value];
        this.results.set(customers);
        if (customers.length === 1) this.select(customers[0]);
        if (customers.length === 0) this.clearSelection('No customers matched your search.');
      },
      error: () => {
        this.clearSelection('');
        this.error.set('Customer could not be found.');
      }
    });
  }

  protected startCreate(): void {
    this.creating.set(true);
    this.selected.set(null);
    this.message.set('');
    this.error.set('');
    this.form.reset({ title: 'Ms', countryCode: 'GB' });
  }

  protected select(customer: Customer): void {
    this.creating.set(false);
    this.selected.set(customer);
    this.message.set('');
    this.error.set('');
    this.form.reset({
      ...customer.details,
      addressLine2: customer.details.addressLine2 ?? '',
      region: customer.details.region ?? '',
      phone: customer.details.phone ?? ''
    });
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.error.set('');
    const current = this.selected();
    const request = this.creating()
      ? this.api.create({ details: formToDetails(this.form), sourceSystem: 'modern', sourceIdentifier: null })
      : this.api.update(current!.id, formToDetails(this.form), current!.version);
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: customer => {
        this.select(customer);
        this.results.update(values => [customer, ...values.filter(value => value.id !== customer.id)]);
        this.message.set(`Customer ${customer.id} saved.`);
      },
      error: (response: HttpErrorResponse) => this.error.set(response.status === 409
        ? 'This customer changed in another session. Search again before saving.'
        : 'Customer could not be saved. Check the form and try again.')
    });
  }

  protected retire(): void {
    const customer = this.selected();
    if (!customer || customer.status !== 'active' || !confirm(`Retire customer ${customer.id}?`)) return;
    this.saving.set(true);
    this.api.retire(customer.id, customer.version).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        this.results.update(values => values.filter(value => value.id !== customer.id));
        this.clearSelection(`Customer ${customer.id} retired.`);
      },
      error: () => this.error.set('Customer cannot be retired while accounts or obligations remain.')
    });
  }

  private clearSelection(message: string): void {
    this.selected.set(null);
    this.creating.set(false);
    this.form.reset({ title: 'Ms', countryCode: 'GB' });
    this.message.set(message);
  }
}
