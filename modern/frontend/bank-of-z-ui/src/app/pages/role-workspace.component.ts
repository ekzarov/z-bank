import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({ selector: 'app-role-workspace', template: `<section class="page"><p class="eyebrow">Authorized workspace</p><h1>{{ title }}</h1><p>This workspace is ready for the next migration slice.</p></section>` })
export class RoleWorkspaceComponent {
  protected readonly title = inject(ActivatedRoute).snapshot.data['title'] as string;
}
