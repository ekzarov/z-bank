import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { SessionService } from '../core/session.service';

@Component({
  selector: 'app-sign-in',
  imports: [FormsModule],
  template: `
    <section class="page"><form class="panel" (ngSubmit)="submit()">
      <p class="eyebrow">Secure access</p><h1>Sign in to Bank of Z</h1>
      <p>Use your assigned customer or staff credentials.</p>
      <label for="userName">User name</label>
      <input id="userName" name="userName" [(ngModel)]="userName" autocomplete="username" required />
      <label for="password">Password</label>
      <input id="password" name="password" type="password" [(ngModel)]="password" autocomplete="current-password" required />
      @if (error()) { <p class="error" role="alert">The supplied credentials could not be accepted.</p> }
      <button type="submit" [disabled]="busy()">{{ busy() ? 'Signing in...' : 'Sign in' }}</button>
    </form></section>
  `,
  styles: [`
    form { margin: 0 auto; } label { display: block; margin: 18px 0 6px; font-weight: 600; }
    input { width: 100%; min-height: 44px; padding: 8px 10px; border: 1px solid #92a8b4; border-radius: 4px; }
    button { width: 100%; min-height: 44px; margin-top: 24px; border: 0; border-radius: 4px; color: white; background: #176b73; font-weight: 700; cursor: pointer; }
    button:disabled { opacity: .65; } .error { margin-top: 16px; color: #a51f2d; }
  `]
})
export class SignInComponent {
  private readonly sessions = inject(SessionService);
  private readonly router = inject(Router);
  protected userName = '';
  protected password = '';
  protected readonly busy = signal(false);
  protected readonly error = signal(false);

  protected submit(): void {
    this.error.set(false);
    this.busy.set(true);
    this.sessions.login({ userName: this.userName, password: this.password, rememberMe: false })
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({ next: () => void this.router.navigateByUrl('/'), error: () => this.error.set(true) });
  }
}
