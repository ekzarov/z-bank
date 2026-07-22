import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { NotFoundComponent } from './not-found.component';
import { UnavailableComponent } from './unavailable.component';

describe('error pages', () => {
  it('renders a clear not-found view', async () => {
    await TestBed.configureTestingModule({ imports: [NotFoundComponent], providers: [provideRouter([])] }).compileComponents();
    const fixture = TestBed.createComponent(NotFoundComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Page not found');
  });

  it('renders a recoverable unavailable view without diagnostics', async () => {
    await TestBed.configureTestingModule({ imports: [UnavailableComponent], providers: [provideRouter([])] }).compileComponents();
    const fixture = TestBed.createComponent(UnavailableComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Please try again');
    expect(fixture.nativeElement.textContent).not.toContain('stack');
  });
});
