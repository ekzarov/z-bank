import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { firstValueFrom, of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { authGuard } from './auth.guard';
import { SessionService } from './session.service';

describe('authGuard', () => {
  it('allows an existing session', () => {
    const session = signal({ isAuthenticated: true, userName: 'customer', customerId: '1000000001', roles: ['Customer'] });
    TestBed.configureTestingModule({
      providers: [
        { provide: SessionService, useValue: { session, unavailable: signal(false), load: vi.fn() } },
        { provide: Router, useValue: { createUrlTree: vi.fn() } }
      ]
    });

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));

    expect(result).toBe(true);
  });

  it('redirects when loading finds no session', async () => {
    const session = signal(null);
    const redirect = {} as UrlTree;
    TestBed.configureTestingModule({
      providers: [
        { provide: SessionService, useValue: { session, unavailable: signal(false), load: vi.fn(() => of(null)) } },
        { provide: Router, useValue: { createUrlTree: vi.fn(() => redirect) } }
      ]
    });

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));

    expect(await firstValueFrom(result as ReturnType<typeof of>)).toBe(redirect);
  });

  it('redirects to the unavailable route when session loading reports an outage', async () => {
    const session = signal(null);
    const unavailable = signal(true);
    const redirect = {} as UrlTree;
    const createUrlTree = vi.fn(() => redirect);
    TestBed.configureTestingModule({
      providers: [
        { provide: SessionService, useValue: { session, unavailable, load: vi.fn(() => of(null)) } },
        { provide: Router, useValue: { createUrlTree } }
      ]
    });

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));

    expect(await firstValueFrom(result as ReturnType<typeof of>)).toBe(redirect);
    expect(createUrlTree).toHaveBeenCalledWith(['/unavailable']);
  });
});
