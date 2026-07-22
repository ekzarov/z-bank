import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({ selector: 'app-unavailable', imports: [RouterLink], template: `<section class="page"><div class="panel"><p class="eyebrow">Service unavailable</p><h1>We could not complete that request</h1><p>Please try again. No sensitive diagnostic details were exposed.</p><a routerLink="/">Try again</a></div></section>` })
export class UnavailableComponent {}
