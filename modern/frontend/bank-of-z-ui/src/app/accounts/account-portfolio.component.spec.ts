import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AccountApiService } from './account-api.service';
import { AccountPortfolioComponent } from './account-portfolio.component';

describe('AccountPortfolioComponent', () => {
  const api = { list: vi.fn(), create: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    api.list.mockReturnValue(of({ items: [account], page: 1, pageSize: 50, total: 1 }));
    TestBed.configureTestingModule({
      imports: [AccountPortfolioComponent],
      providers: [provideRouter([]), { provide: AccountApiService, useValue: api }]
    });
  });

  it('renders account identity and both balances', () => {
    const fixture = TestBed.createComponent(AccountPortfolioComponent);
    fixture.componentRef.setInput('customerId', '1000000001');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('100000 / 10000001');
    expect(fixture.nativeElement.textContent).toContain('available');
  });

  it('creates an account only from operator controls', () => {
    api.create.mockReturnValue(of(account));
    const fixture = TestBed.createComponent(AccountPortfolioComponent);
    fixture.componentRef.setInput('customerId', '1000000001');
    fixture.componentRef.setInput('operator', true);
    fixture.detectChanges();
    const component = fixture.componentInstance as any;

    component.startCreate();
    component.create();

    expect(api.create).toHaveBeenCalledWith('1000000001', expect.objectContaining({ type: 'current' }));
    expect(component.message()).toContain('10000001');
  });

  it('requests the next bounded page when closed accounts exceed one page', () => {
    api.list.mockReturnValue(of({ items: [account], page: 1, pageSize: 10, total: 11 }));
    const fixture = TestBed.createComponent(AccountPortfolioComponent);
    fixture.componentRef.setInput('customerId', '1000000001');
    fixture.detectChanges();
    const component = fixture.componentInstance as any;

    component.nextPage();

    expect(api.list).toHaveBeenLastCalledWith('1000000001', 2, 10);
  });
});

const account = {
  id: '10000001', customerId: '1000000001', sortCode: '100000', type: 'current', interestRate: 0,
  overdraftLimit: 500, currency: 'GBP', actualBalance: 125, availableBalance: 625,
  openedOn: '2026-07-22', lastStatementOn: '2026-07-22', nextStatementOn: '2026-08-22',
  status: 'active', sourceSystem: 'modern', sourceIdentifier: null, rawSourceType: null, version: 'version'
} as const;
