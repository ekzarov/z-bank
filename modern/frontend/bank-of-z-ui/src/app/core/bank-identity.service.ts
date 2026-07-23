import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';

interface BankIdentity {
  displayName: string;
  sortCode: string;
}

@Injectable({ providedIn: 'root' })
export class BankIdentityService {
  private readonly http = inject(HttpClient);
  readonly displayName = signal('Bank of Z');
  readonly sortCode = signal('100000');

  load(): void {
    this.http.get<BankIdentity>('api/configuration/bank').subscribe({
      next: identity => {
        this.displayName.set(identity.displayName);
        this.sortCode.set(identity.sortCode);
      },
      error: () => undefined
    });
  }
}
