import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { AccountApiService } from './account-api.service';
import { TransactionHistoryComponent } from './transaction-history.component';

describe('TransactionHistoryComponent', () => {
  const parameters = new BehaviorSubject(convertToParamMap({ id: '10000001' }));
  const api = { history: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    parameters.next(convertToParamMap({ id: '10000001' }));
    api.history.mockReturnValue(of({ items: [transaction], pageSize: 50, nextCursor: null }));
    TestBed.configureTestingModule({
      imports: [TransactionHistoryComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: parameters.asObservable() } },
        { provide: AccountApiService, useValue: api }
      ]
    });
  });

  it('renders newest history and links to detail', () => {
    const fixture = TestBed.createComponent(TransactionHistoryComponent);
    fixture.detectChanges();

    expect(api.history).toHaveBeenCalledWith('10000001', undefined, undefined, undefined);
    expect(fixture.nativeElement.textContent).toContain('Internal transfer sent');
    expect(fixture.nativeElement.querySelector('.transaction-row').getAttribute('href'))
      .toContain(`/accounts/10000001/transactions/${transaction.reference}`);
  });

  it('applies UTC date filters and appends the next cursor page', () => {
    api.history
      .mockReturnValueOnce(of({ items: [transaction], pageSize: 50, nextCursor: 'next' }))
      .mockReturnValueOnce(of({ items: [{ ...transaction, reference: 'b'.repeat(32) }], pageSize: 50, nextCursor: null }));
    const fixture = TestBed.createComponent(TransactionHistoryComponent);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.load-more').click();
    fixture.detectChanges();
    expect(api.history).toHaveBeenLastCalledWith('10000001', undefined, undefined, 'next');
    expect(fixture.nativeElement.querySelectorAll('.transaction-row')).toHaveLength(2);

    const from = fixture.nativeElement.querySelector('input[formControlName="from"]');
    const to = fixture.nativeElement.querySelector('input[formControlName="to"]');
    from.value = '2026-07-01';
    from.dispatchEvent(new Event('input'));
    to.value = '2026-08-01';
    to.dispatchEvent(new Event('input'));
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    expect(api.history).toHaveBeenLastCalledWith(
      '10000001', '2026-07-01T00:00:00Z', '2026-08-01T00:00:00Z', undefined);
  });

  it('shows an empty state', () => {
    api.history.mockReturnValue(of({ items: [], pageSize: 50, nextCursor: null }));
    const fixture = TestBed.createComponent(TransactionHistoryComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No transactions');
  });
});

const transaction = {
  reference: 'a'.repeat(32),
  accountId: '10000001',
  direction: 'withdrawal',
  amount: 25,
  currency: 'GBP',
  resultingActualBalance: 75,
  resultingAvailableBalance: 75,
  status: 'booked',
  description: 'Internal transfer sent',
  bookedAt: '2026-07-23T10:00:00Z',
  transferCorrelationId: 'c'.repeat(32),
  relatedTransferReference: 'd'.repeat(32),
  sourceSystem: 'modern',
  sourceIdentifier: null
} as const;
