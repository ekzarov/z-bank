import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { roleGuard } from './core/role.guard';
import { DashboardComponent } from './pages/dashboard.component';
import { NotFoundComponent } from './pages/not-found.component';
import { RoleWorkspaceComponent } from './pages/role-workspace.component';
import { SignInComponent } from './pages/sign-in.component';
import { UnavailableComponent } from './pages/unavailable.component';
import { CustomerProfileComponent } from './customers/customer-profile.component';
import { CustomerWorkspaceComponent } from './customers/customer-workspace.component';

export const routes: Routes = [
  { path: 'sign-in', component: SignInComponent },
  { path: 'unavailable', component: UnavailableComponent },
  { path: '', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'customer', component: CustomerProfileComponent, canActivate: [authGuard, roleGuard], data: { role: 'Customer' } },
  { path: 'operations', component: CustomerWorkspaceComponent, canActivate: [authGuard, roleGuard], data: { role: 'Operator' } },
  { path: 'administration', component: RoleWorkspaceComponent, canActivate: [authGuard, roleGuard], data: { role: 'Administrator', title: 'Access administration' } },
  { path: '**', component: NotFoundComponent }
];
