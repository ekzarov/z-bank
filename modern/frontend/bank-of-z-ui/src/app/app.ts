import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { navigationForRoles } from './core/navigation-items';
import { SessionService } from './core/session.service';
import { BankIdentityService } from './core/bank-identity.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly sessionService = inject(SessionService);
  protected readonly bankIdentity = inject(BankIdentityService);
  private readonly router = inject(Router);
  protected readonly navigation = computed(() =>
    navigationForRoles(this.sessionService.session()?.roles ?? []));

  constructor() {
    this.bankIdentity.load();
  }

  protected logout(): void {
    this.sessionService.logout().subscribe({
      next: () => void this.router.navigateByUrl('/sign-in')
    });
  }
}
