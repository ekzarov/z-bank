import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { StatementApiService } from './statement-api.service';
import { StatementComponent } from './statement.component';
import { StatementView } from './statement.model';

describe('StatementComponent', () => {
  const parameters = new BehaviorSubject(convertToParamMap({ id: '10000001' }));
  const api = { generate: vi.fn(), find: vi.fn() };
  const router = { navigate: vi.fn().mockResolvedValue(true) };

  beforeEach(() => {
    vi.clearAllMocks();
    parameters.next(convertToParamMap({ id: '10000001' }));
    api.generate.mockReturnValue(of(statement));
    api.find.mockReturnValue(of(statement));
    TestBed.configureTestingModule({
      imports: [StatementComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { paramMap: parameters.asObservable() } },
        { provide: Router, useValue: router },
        { provide: StatementApiService, useValue: api }
      ]
    });
  });

  it('generates and renders complete statement content', () => {
    const fixture = TestBed.createComponent(StatementComponent);
    fixture.detectChanges();
    setPeriod(fixture.nativeElement, 2026, 6);
    fixture.nativeElement.querySelector('.period-form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(api.generate).toHaveBeenCalledWith('10000001', 2026, 6);
    expect(router.navigate).toHaveBeenCalledWith(
      ['/accounts', '10000001', 'statements', statement.generationId],
      { replaceUrl: true });
    expect(fixture.nativeElement.querySelector('[aria-label="Monthly statement"]').textContent)
      .toContain('Monthly account statement');
    expect(fixture.nativeElement.querySelector('[aria-label="Statement summary"]').textContent)
      .toContain('Closing balance');
    expect(fixture.nativeElement.querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('loads an immutable statement deep link and renders the empty-period state', () => {
    api.find.mockReturnValue(of({ ...statement, transactionCount: 0, transactions: [] }));
    parameters.next(convertToParamMap({
      id: '10000001',
      generationId: statement.generationId
    }));

    const fixture = TestBed.createComponent(StatementComponent);
    fixture.detectChanges();

    expect(api.find).toHaveBeenCalledWith('10000001', statement.generationId);
    expect(fixture.nativeElement.textContent).toContain('No transactions for this period');
  });

  it('prints the rendered statement', () => {
    const print = vi.spyOn(window, 'print').mockImplementation(() => undefined);
    const fixture = TestBed.createComponent(StatementComponent);
    fixture.detectChanges();
    setPeriod(fixture.nativeElement, 2026, 6);
    fixture.nativeElement.querySelector('.period-form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.page-heading button').click();

    expect(print).toHaveBeenCalledOnce();
    print.mockRestore();
  });
});

function setPeriod(element: HTMLElement, year: number, month: number): void {
  const yearInput = element.querySelector<HTMLInputElement>('input[formControlName="year"]')!;
  yearInput.value = String(year);
  yearInput.dispatchEvent(new Event('input'));
  const monthSelect = element.querySelector<HTMLSelectElement>('select[formControlName="month"]')!;
  monthSelect.selectedIndex = month - 1;
  monthSelect.dispatchEvent(new Event('change'));
}

const statement: StatementView = {
  generationId: '11111111-1111-1111-1111-111111111111',
  accountId: '10000001',
  customerId: '1000000001',
  year: 2026,
  month: 6,
  periodStartUtc: '2026-06-01T00:00:00Z',
  periodEndUtc: '2026-07-01T00:00:00Z',
  statementDate: '2026-06-30',
  generatedAt: '2026-07-01T10:00:00Z',
  dataAsOf: '2026-07-01T10:00:00Z',
  customerName: 'Ms Jane Customer',
  customerAddress: '1 Bank Street\nLondon EC1A 1AA\nGB',
  customerPhone: '+44 20 0000 0000',
  sortCode: '100000',
  accountType: 'current',
  currency: 'GBP',
  interestRate: 1.25,
  overdraftLimit: 500,
  openingBalance: 100,
  totalCredits: 25,
  totalDebits: 10,
  closingBalance: 115,
  availableBalance: 115,
  transactionCount: 1,
  transactions: [{
    bookedAt: '2026-06-15T09:30:00Z',
    direction: 'deposit',
    reference: '0123456789abcdef0123456789abcdef',
    description: 'Cash deposit',
    amount: 25
  }]
};
