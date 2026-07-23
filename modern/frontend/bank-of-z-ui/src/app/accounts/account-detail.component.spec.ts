import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { SessionService } from '../core/session.service';
import { AccountApiService } from './account-api.service';
import { AccountDetailComponent } from './account-detail.component';

describe('AccountDetailComponent', () => {
  const parameters = new BehaviorSubject(convertToParamMap({ id: '10000001' }));
  const api = { find: vi.fn(), update: vi.fn(), close: vi.fn(), bookCash: vi.fn(), transfer: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    parameters.next(convertToParamMap({ id: '10000001' }));
    api.find.mockImplementation((id: string) => of({ ...account, id }));
    TestBed.configureTestingModule({
      imports: [AccountDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: parameters.asObservable() } },
        { provide: AccountApiService, useValue: api },
        { provide: SessionService, useValue: { session: signal({ roles: ['Operator'] }) } }
      ]
    });
  });

  it('reloads when Angular reuses the route for another account', () => {
    const fixture = TestBed.createComponent(AccountDetailComponent);
    fixture.detectChanges();
    parameters.next(convertToParamMap({ id: '10000002' }));
    fixture.detectChanges();

    expect(api.find).toHaveBeenNthCalledWith(1, '10000001');
    expect(api.find).toHaveBeenNthCalledWith(2, '10000002');
    expect(fixture.nativeElement.textContent).toContain('10000002');
  });

  it('rejects an invalid deep link without calling the API', () => {
    parameters.next(convertToParamMap({ id: 'invalid' }));
    const fixture = TestBed.createComponent(AccountDetailComponent);
    fixture.detectChanges();

    expect(api.find).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('invalid');
  });

  it('books cash and reloads the account with the resulting balance', () => {
    api.bookCash.mockReturnValue(of({ reference: '0123456789abcdef0123456789abcdef', availableBalance: 125, currency: 'GBP' }));
    const fixture = TestBed.createComponent(AccountDetailComponent);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('select[formControlName="direction"]').value = 'deposit';
    fixture.nativeElement.querySelector('select[formControlName="direction"]').dispatchEvent(new Event('change'));
    const amount = fixture.nativeElement.querySelector('input[formControlName="amount"]');
    amount.value = '125';
    amount.dispatchEvent(new Event('input'));
    fixture.nativeElement.querySelector('.cash-panel form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(api.bookCash).toHaveBeenCalledWith('10000001', 'deposit', 125);
    expect(api.find).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent).toContain('0123456789abcdef0123456789abcdef');
  });

  it('books an internal transfer and reloads the source balance', () => {
    api.transfer.mockReturnValue(of({
      correlationId: 'abcdef0123456789abcdef0123456789',
      source: { availableBalance: 75, currency: 'GBP' },
      destination: { availableBalance: 25, currency: 'GBP' }
    }));
    const fixture = TestBed.createComponent(AccountDetailComponent);
    fixture.detectChanges();

    const destination = fixture.nativeElement.querySelector('input[formControlName="destinationAccountId"]');
    destination.value = '10000002';
    destination.dispatchEvent(new Event('input'));
    const amount = fixture.nativeElement.querySelector('.transfer-panel input[formControlName="amount"]');
    amount.value = '25';
    amount.dispatchEvent(new Event('input'));
    fixture.nativeElement.querySelector('.transfer-panel form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(api.transfer).toHaveBeenCalledWith('10000001', '10000002', 25);
    expect(api.find).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent)
      .toContain('abcdef0123456789abcdef0123456789');
  });
});

const account = {
  id: '10000001', customerId: '1000000001', sortCode: '100000', type: 'current', interestRate: 0,
  overdraftLimit: 500, currency: 'GBP', actualBalance: 0, availableBalance: 0,
  openedOn: '2026-07-22', lastStatementOn: '2026-07-22', nextStatementOn: '2026-08-22',
  status: 'active', sourceSystem: 'modern', sourceIdentifier: null, rawSourceType: null, version: 'version'
} as const;
