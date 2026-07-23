import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AdministrationApiService } from './administration-api.service';

describe('AdministrationApiService', () => {
  let service: AdministrationApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AdministrationApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('searches users with bounded page parameters', () => {
    service.searchUsers('operator', 2, 20).subscribe(page => expect(page.total).toBe(1));

    const request = http.expectOne(candidate =>
      candidate.url === 'api/administration/users'
      && candidate.params.get('query') === 'operator'
      && candidate.params.get('page') === '2'
      && candidate.params.get('pageSize') === '20');
    expect(request.request.method).toBe('GET');
    request.flush({ items: [user], page: 2, pageSize: 20, total: 1 });
  });

  it('gets csrf before changing user lockout', () => {
    service.changeLockout(user.id, true, user.version).subscribe(updated => expect(updated.isLockedOut).toBe(true));

    http.expectOne('api/session/csrf').flush({ token: 'token' });
    const request = http.expectOne(`api/administration/users/${user.id}/lockout`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ locked: true, version: user.version });
    request.flush({ ...user, isLockedOut: true, version: 'next-version' });
  });

  it('passes only populated audit filters', () => {
    service.searchSecurityAudit({
      eventName: 'user-locked',
      actorOrSubject: '',
      succeeded: 'false',
      from: '',
      to: ''
    }).subscribe(page => expect(page.items).toEqual([]));

    const request = http.expectOne(candidate =>
      candidate.url === 'api/administration/security-audit'
      && candidate.params.get('eventName') === 'user-locked'
      && candidate.params.get('succeeded') === 'false'
      && !candidate.params.has('actorOrSubject'));
    request.flush({ items: [], page: 1, pageSize: 20, total: 0 });
  });
});

const user = {
  id: '34d501d9-cc1d-4ee5-adb2-8291bde1d031',
  userName: 'operator',
  email: 'operator@example.test',
  role: 'Operator' as const,
  customerId: null,
  isLockedOut: false,
  lockoutEnd: null,
  accessFailedCount: 0,
  version: 'version'
};
