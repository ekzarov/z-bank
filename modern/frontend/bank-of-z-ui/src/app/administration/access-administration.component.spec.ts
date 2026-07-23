import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AccessAdministrationComponent } from './access-administration.component';

describe('AccessAdministrationComponent', () => {
  let fixture: ComponentFixture<AccessAdministrationComponent>;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AccessAdministrationComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    fixture = TestBed.createComponent(AccessAdministrationComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne(request => request.url === 'api/administration/users').flush({
      items: [user],
      page: 1,
      pageSize: 20,
      total: 1
    });
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('renders a useful administrator user action @surface:administration @role:Administrator', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('h1')?.textContent).toContain('Access administration');
    expect(element.textContent).toContain('operator');

    (element.querySelector('.result') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(element.textContent).toContain('Save role');
    expect(element.textContent).toContain('Lock user');
  });

  it('loads persistent security events from the audit tab @surface:administration @role:Administrator', () => {
    const element = fixture.nativeElement as HTMLElement;
    const auditTab = [...element.querySelectorAll<HTMLButtonElement>('.tabs button')]
      .find(button => button.textContent?.includes('Security audit'))!;
    auditTab.click();
    fixture.detectChanges();

    http.expectOne(request => request.url === 'api/administration/security-audit').flush({
      items: [{
        id: 1,
        occurredAt: '2026-07-23T18:00:00Z',
        eventName: 'user-locked',
        actorId: 'admin-id',
        subjectId: user.id,
        succeeded: true,
        outcome: 'locked',
        correlationId: 'test-correlation'
      }],
      page: 1,
      pageSize: 20,
      total: 1
    });
    fixture.detectChanges();

    expect(element.textContent).toContain('user-locked');
    expect(element.textContent).toContain('Succeeded');
  });
});

const user = {
  id: '34d501d9-cc1d-4ee5-adb2-8291bde1d031',
  userName: 'operator',
  email: 'operator@example.test',
  role: 'Operator',
  customerId: null,
  isLockedOut: false,
  lockoutEnd: null,
  accessFailedCount: 0,
  version: 'version'
};
