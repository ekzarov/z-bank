import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({ selector: 'app-not-found', imports: [RouterLink], template: `<section class="page"><div class="panel"><p class="eyebrow">404</p><h1>Page not found</h1><p>The address does not match a Bank of Z page.</p><a routerLink="/">Return to overview</a></div></section>` })
export class NotFoundComponent {}
