import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { BulkStatementsComponent } from './bulk-statements.component';
import { StatementApiService } from './statement-api.service';
import { BulkStatementResult } from './statement.model';

describe('BulkStatementsComponent', () => {
  const api = { generateBulk: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    api.generateBulk.mockReturnValue(of(partialResult));
    TestBed.configureTestingModule({
      imports: [BulkStatementsComponent],
      providers: [
        provideRouter([]),
        { provide: StatementApiService, useValue: api }
      ]
    });
  });

  it('shows published, reused, and failed per-account outcomes', () => {
    const fixture = TestBed.createComponent(BulkStatementsComponent);
    fixture.detectChanges();
    setPeriod(fixture.nativeElement, 2026, 6);
    fixture.nativeElement.querySelector('.run-form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(api.generateBulk).toHaveBeenCalledWith(2026, 6, undefined);
    expect(fixture.nativeElement.querySelector('[aria-label="Bulk statement result"]').textContent)
      .toContain('Succeeded');
    expect(fixture.nativeElement.textContent).toContain('Published');
    expect(fixture.nativeElement.textContent).toContain('Reused');
    expect(fixture.nativeElement.textContent).toContain('Source data unavailable');
    expect(fixture.nativeElement.querySelectorAll('a')).toHaveLength(2);
  });

  it('retries only failed accounts and keeps successful results', () => {
    api.generateBulk
      .mockReturnValueOnce(of(partialResult))
      .mockReturnValueOnce(of({
        year: 2026,
        month: 6,
        total: 1,
        succeeded: 1,
        failed: 0,
        accounts: [{
          accountId: '10000003',
          succeeded: true,
          generationId: '33333333-3333-3333-3333-333333333333',
          reused: false,
          error: null
        }]
      }));
    const fixture = TestBed.createComponent(BulkStatementsComponent);
    fixture.detectChanges();
    setPeriod(fixture.nativeElement, 2026, 6);
    fixture.nativeElement.querySelector('.run-form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    fixture.nativeElement.querySelector('.result-heading button').click();
    fixture.detectChanges();

    expect(api.generateBulk).toHaveBeenLastCalledWith(2026, 6, ['10000003']);
    expect(fixture.nativeElement.textContent).toContain('Succeeded');
    expect(fixture.nativeElement.textContent).not.toContain('Source data unavailable');
    expect(fixture.nativeElement.querySelectorAll('.account-result')).toHaveLength(3);
    expect(fixture.nativeElement.querySelectorAll('.account-result.failed')).toHaveLength(0);
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

const partialResult: BulkStatementResult = {
  year: 2026,
  month: 6,
  total: 3,
  succeeded: 2,
  failed: 1,
  accounts: [
    {
      accountId: '10000001',
      succeeded: true,
      generationId: '11111111-1111-1111-1111-111111111111',
      reused: false,
      error: null
    },
    {
      accountId: '10000002',
      succeeded: true,
      generationId: '22222222-2222-2222-2222-222222222222',
      reused: true,
      error: null
    },
    {
      accountId: '10000003',
      succeeded: false,
      generationId: null,
      reused: false,
      error: 'Source data unavailable'
    }
  ]
};
