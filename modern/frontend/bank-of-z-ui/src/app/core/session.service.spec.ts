import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { SessionService } from './session.service';

describe('SessionService', () => {
  it('loads an authenticated session', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(SessionService);
    const http = TestBed.inject(HttpTestingController);

    service.load().subscribe();
    http.expectOne('api/session').flush({
      isAuthenticated: true,
      userName: 'customer',
      customerId: '1000000001',
      roles: ['Customer']
    });

    expect(service.session()?.userName).toBe('customer');
    expect(service.session()?.customerId).toMatch(/^\d+$/);
    http.verify();
  });

  it('gets a CSRF token before login and stores the returned session', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(SessionService);
    const http = TestBed.inject(HttpTestingController);

    service.login({ userName: 'operator', password: 'secret', rememberMe: false }).subscribe();
    http.expectOne('api/session/csrf').flush({ token: 'request-token' });
    http.expectOne(request => request.url === 'api/session/login' && request.method === 'POST').flush({
      isAuthenticated: true,
      userName: 'operator',
      customerId: null,
      roles: ['Operator']
    });

    expect(service.session()?.roles).toEqual(['Operator']);
    http.verify();
  });

  it('revokes local session state after logout succeeds', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(SessionService);
    const http = TestBed.inject(HttpTestingController);
    service.session.set({ isAuthenticated: true, userName: 'operator', customerId: null, roles: ['Operator'] });

    service.logout().subscribe();
    http.expectOne('api/session/csrf').flush({ token: 'request-token' });
    http.expectOne(request => request.url === 'api/session/logout' && request.method === 'POST').flush(null);

    expect(service.session()).toBeNull();
    http.verify();
  });

  it('marks the API unavailable and clears the session for a server outage', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(SessionService);
    const http = TestBed.inject(HttpTestingController);

    service.load().subscribe();
    http.expectOne('api/session').flush({}, { status: 503, statusText: 'Unavailable' });

    expect(service.session()).toBeNull();
    expect(service.unavailable()).toBe(true);
    http.verify();
  });

  it('does not classify an unauthorized session response as an outage', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(SessionService);
    const http = TestBed.inject(HttpTestingController);

    service.load().subscribe();
    http.expectOne('api/session').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(service.session()).toBeNull();
    expect(service.unavailable()).toBe(false);
    http.verify();
  });
});
