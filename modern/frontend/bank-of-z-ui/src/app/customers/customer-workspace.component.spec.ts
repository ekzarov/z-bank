import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CustomerApiService } from './customer-api.service';
import { Customer } from './customer.model';
import { CustomerWorkspaceComponent } from './customer-workspace.component';

describe('CustomerWorkspaceComponent', () => {
  const api = {
    search: vi.fn(), find: vi.fn(), create: vi.fn(), update: vi.fn(), retire: vi.fn()
  };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [CustomerWorkspaceComponent],
      providers: [{ provide: CustomerApiService, useValue: api }]
    });
  });

  it('clears stale details and leaves loading after a failed lookup', () => {
    api.search.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const fixture = TestBed.createComponent(CustomerWorkspaceComponent);
    const component = fixture.componentInstance as any;
    component.selected.set(customer);
    component.query.set('missing name');

    component.search();

    expect(component.selected()).toBeNull();
    expect(component.loading()).toBe(false);
    expect(component.error()).toContain('not be found');
  });

  it('redisplays the created customer and reports its immutable id', () => {
    api.create.mockReturnValue(of(customer));
    const fixture = TestBed.createComponent(CustomerWorkspaceComponent);
    const component = fixture.componentInstance as any;
    component.startCreate();
    component.form.setValue({
      title: 'Ms', firstName: 'Jamie', lastName: 'Customer', dateOfBirth: '1990-05-12',
      addressLine1: '1 Test Street', addressLine2: '', city: 'London', region: '',
      postalCode: 'EC1A 1AA', countryCode: 'GB', email: 'customer@example.test', phone: ''
    });

    component.save();

    expect(component.selected()?.id).toBe('1000000001');
    expect(component.creating()).toBe(false);
    expect(component.message()).toContain('1000000001');
  });

  it('keeps mutation actions disabled without a current customer', () => {
    const fixture = TestBed.createComponent(CustomerWorkspaceComponent);
    fixture.detectChanges();

    const buttons = [...fixture.nativeElement.querySelectorAll('.actions button')] as HTMLButtonElement[];
    expect(buttons.every(button => button.disabled)).toBe(true);
  });
});

const customer: Customer = {
  id: '1000000001', sortCode: '100000', status: 'active', creditScore: 720,
  creditReviewDate: '2026-08-12', sourceSystem: 'modern', sourceIdentifier: 'test',
  createdAt: '2026-07-22T00:00:00Z', updatedAt: '2026-07-22T00:00:00Z', version: 'version',
  details: {
    title: 'Ms', firstName: 'Jamie', lastName: 'Customer', dateOfBirth: '1990-05-12',
    addressLine1: '1 Test Street', addressLine2: null, city: 'London', region: null,
    postalCode: 'EC1A 1AA', countryCode: 'GB', email: 'customer@example.test', phone: null
  }
};
