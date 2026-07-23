import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { AccountApiService } from './account-api.service';
import { TransactionHistoryDetailComponent } from './transaction-history-detail.component';

describe('TransactionHistoryDetailComponent', () => {
  const reference = 'a'.repeat(32);
  const parameters = new BehaviorSubject(convertToParamMap({ id: '10000001', reference }));
  const api = { transaction: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    parameters.next(convertToParamMap({ id: '10000001', reference }));
    api.transaction.mockReturnValue(of({
      reference,
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
    }));
    TestBed.configureTestingModule({
      imports: [TransactionHistoryDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: parameters.asObservable() } },
        { provide: AccountApiService, useValue: api }
      ]
    });
  });

  it('shows immutable transaction details and related transfer evidence', () => {
    const fixture = TestBed.createComponent(TransactionHistoryDetailComponent);
    fixture.detectChanges();

    expect(api.transaction).toHaveBeenCalledWith('10000001', reference);
    expect(fixture.nativeElement.textContent).toContain('Internal transfer sent');
    expect(fixture.nativeElement.textContent).toContain('Related transfer');
    expect(fixture.nativeElement.textContent).toContain('Correlation');
  });
});
