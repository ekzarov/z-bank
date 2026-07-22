import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';
import { roleGuard } from './role.guard';
import { SessionService } from './session.service';

describe('roleGuard', () => {
  it('allows a matching role and redirects a wrong role', () => {
    const session = signal({ isAuthenticated: true, userName: 'customer', customerId: '1000000001', roles: ['Customer'] });
    const redirect = {} as UrlTree;
    const router = { createUrlTree: vi.fn(() => redirect) };
    TestBed.configureTestingModule({
      providers: [
        { provide: SessionService, useValue: { session } },
        { provide: Router, useValue: router }
      ]
    });

    const allowedRoute = { data: { role: 'Customer' } } as unknown as ActivatedRouteSnapshot;
    const deniedRoute = { data: { role: 'Operator' } } as unknown as ActivatedRouteSnapshot;
    const state = {} as RouterStateSnapshot;

    expect(TestBed.runInInjectionContext(() => roleGuard(allowedRoute, state))).toBe(true);
    expect(TestBed.runInInjectionContext(() => roleGuard(deniedRoute, state))).toBe(redirect);
  });
});
