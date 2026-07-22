import { Component, inject, signal } from '@angular/core';
import { CustomerApiService } from './customer-api.service';
import { Customer } from './customer.model';

@Component({
  selector: 'app-customer-profile',
  template: `
    <section class="page">
      <p class="eyebrow">My banking</p><h1>My profile</h1>
      @if (customer(); as value) {
        <div class="profile">
          <header><div><h2>{{ value.details.firstName }} {{ value.details.lastName }}</h2><p>{{ value.id }}</p></div><span>{{ value.status }}</span></header>
          <dl><div><dt>Email</dt><dd>{{ value.details.email }}</dd></div><div><dt>Phone</dt><dd>{{ value.details.phone || 'Not provided' }}</dd></div><div><dt>Address</dt><dd>{{ value.details.addressLine1 }}, {{ value.details.city }}, {{ value.details.postalCode }}</dd></div><div><dt>Sort code</dt><dd>{{ value.sortCode }}</dd></div></dl>
          <section class="accounts"><h2>Accounts</h2><p>Account summaries will appear here when the account-management slice is delivered.</p></section>
        </div>
      } @else if (error()) { <p class="error" role="alert">Your customer profile is unavailable.</p> }
    </section>`,
  styles: [`
    .profile { max-width: 760px; padding: 28px; border: 1px solid #cbd8de; border-radius: 6px; background: #fff; }
    header { display:flex; justify-content:space-between; gap:20px; border-bottom:1px solid #dce4e8; }
    header span { text-transform:capitalize; color:#285c3c; }
    dl { display:grid; grid-template-columns:1fr 1fr; gap:20px; margin:24px 0; }
    dt { color:#637985; font-size:.8rem; } dd { margin:4px 0 0; }
    .accounts { padding-top:20px; border-top:1px solid #dce4e8; }
    .accounts p { color:#637985; }
    .error { padding:14px; border-left:4px solid #b54840; background:#fff; }
  `]
})
export class CustomerProfileComponent {
  private readonly api = inject(CustomerApiService);
  protected readonly customer = signal<Customer | null>(null);
  protected readonly error = signal(false);

  constructor() {
    this.api.me().subscribe({
      next: customer => this.customer.set(customer),
      error: () => this.error.set(true)
    });
  }
}
