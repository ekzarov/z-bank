import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AdministrationApiService } from './administration-api.service';
import {
  AdministrationRole,
  AdministrationUser,
  SecurityAuditEntry,
  SecurityAuditFilters
} from './administration.model';

@Component({
  selector: 'app-access-administration',
  imports: [ReactiveFormsModule],
  templateUrl: './access-administration.component.html',
  styleUrl: './access-administration.component.scss'
})
export class AccessAdministrationComponent {
  private readonly api = inject(AdministrationApiService);
  protected readonly roles: AdministrationRole[] = ['Customer', 'Operator', 'Administrator'];
  protected readonly activeTab = signal<'users' | 'audit'>('users');
  protected readonly query = signal('');
  protected readonly users = signal<AdministrationUser[]>([]);
  protected readonly selected = signal<AdministrationUser | null>(null);
  protected readonly auditEntries = signal<SecurityAuditEntry[]>([]);
  protected readonly userTotal = signal(0);
  protected readonly auditTotal = signal(0);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly message = signal('');
  protected readonly error = signal('');
  protected readonly createOpen = signal(false);

  protected readonly createForm = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(64)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(12)] }),
    role: new FormControl<AdministrationRole>('Customer', { nonNullable: true }),
    customerId: new FormControl('')
  });

  protected readonly roleForm = new FormGroup({
    role: new FormControl<AdministrationRole>('Customer', { nonNullable: true }),
    customerId: new FormControl('')
  });

  protected readonly auditForm = new FormGroup({
    eventName: new FormControl('', { nonNullable: true }),
    actorOrSubject: new FormControl('', { nonNullable: true }),
    succeeded: new FormControl<'' | 'true' | 'false'>('', { nonNullable: true }),
    from: new FormControl('', { nonNullable: true }),
    to: new FormControl('', { nonNullable: true })
  });

  constructor() {
    this.searchUsers();
  }

  protected setTab(tab: 'users' | 'audit'): void {
    this.activeTab.set(tab);
    this.clearFeedback();
    if (tab === 'audit' && this.auditEntries().length === 0) this.searchAudit();
  }

  protected searchUsers(): void {
    this.loading.set(true);
    this.clearFeedback();
    this.api.searchUsers(this.query()).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: page => {
        this.users.set(page.items);
        this.userTotal.set(page.total);
        const selectedId = this.selected()?.id;
        const refreshed = page.items.find(user => user.id === selectedId) ?? null;
        if (selectedId) this.selectUser(refreshed);
      },
      error: response => this.handleLoadError(response, 'Users could not be loaded.')
    });
  }

  protected selectUser(user: AdministrationUser | null): void {
    this.selected.set(user);
    this.createOpen.set(false);
    this.clearFeedback();
    this.roleForm.reset({
      role: user?.role ?? 'Customer',
      customerId: user?.customerId ?? ''
    });
  }

  protected startCreate(): void {
    this.selected.set(null);
    this.createOpen.set(true);
    this.clearFeedback();
    this.createForm.reset({ role: 'Customer' });
  }

  protected createUser(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const value = this.createForm.getRawValue();
    this.saving.set(true);
    this.clearFeedback();
    this.api.createUser({
      ...value,
      customerId: value.role === 'Customer' ? value.customerId?.trim() || null : null
    }).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: user => {
        this.users.update(users => [user, ...users]);
        this.userTotal.update(total => total + 1);
        this.selectUser(user);
        this.message.set(`User ${user.userName} created.`);
      },
      error: response => this.handleMutationError(response, 'User could not be created.')
    });
  }

  protected changeRole(): void {
    const user = this.selected();
    if (!user || this.roleForm.invalid) return;
    const value = this.roleForm.getRawValue();
    if (!confirm(`Change ${user.userName}'s role to ${value.role}?`)) return;
    this.saving.set(true);
    this.clearFeedback();
    this.api.changeRole(
      user.id,
      value.role,
      value.role === 'Customer' ? value.customerId?.trim() || null : null,
      user.version
    ).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: updated => this.applyUpdatedUser(updated, `Role changed to ${updated.role}.`),
      error: response => this.handleMutationError(response, 'Role could not be changed.')
    });
  }

  protected changeLockout(): void {
    const user = this.selected();
    if (!user) return;
    const locked = !user.isLockedOut;
    const action = locked ? 'lock' : 'unlock';
    if (!confirm(`${action[0].toUpperCase()}${action.slice(1)} ${user.userName}?`)) return;
    this.saving.set(true);
    this.clearFeedback();
    this.api.changeLockout(user.id, locked, user.version)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: updated => this.applyUpdatedUser(updated, `User ${updated.userName} ${locked ? 'locked' : 'unlocked'}.`),
        error: response => this.handleMutationError(response, `User could not be ${locked ? 'locked' : 'unlocked'}.`)
      });
  }

  protected searchAudit(): void {
    this.loading.set(true);
    this.clearFeedback();
    this.api.searchSecurityAudit(this.auditForm.getRawValue() as SecurityAuditFilters)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: page => {
          this.auditEntries.set(page.items);
          this.auditTotal.set(page.total);
        },
        error: response => this.handleLoadError(response, 'Security audit could not be loaded.')
      });
  }

  private applyUpdatedUser(user: AdministrationUser, message: string): void {
    this.users.update(users => users.map(current => current.id === user.id ? user : current));
    this.selectUser(user);
    this.message.set(message);
  }

  private handleMutationError(response: HttpErrorResponse, fallback: string): void {
    if (response.status === 409) {
      this.error.set('The user changed or the requested action would remove required administrator access. Refresh and try again.');
      return;
    }
    this.error.set(response.error?.title ?? fallback);
  }

  private handleLoadError(response: HttpErrorResponse, fallback: string): void {
    this.error.set(response.status === 403
      ? 'Administrator access is required.'
      : fallback);
  }

  private clearFeedback(): void {
    this.message.set('');
    this.error.set('');
  }
}
