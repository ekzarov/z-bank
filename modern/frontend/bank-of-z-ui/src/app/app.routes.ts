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
import { AccountDetailComponent } from './accounts/account-detail.component';
import { TransactionHistoryComponent } from './accounts/transaction-history.component';
import { TransactionHistoryDetailComponent } from './accounts/transaction-history-detail.component';
import { BulkStatementsComponent } from './statements/bulk-statements.component';
import { StatementComponent } from './statements/statement.component';

export const routes: Routes = [
  { path: 'sign-in', component: SignInComponent },
  { path: 'unavailable', component: UnavailableComponent },
  { path: '', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'customer', component: CustomerProfileComponent, canActivate: [authGuard, roleGuard], data: { role: 'Customer' } },
  { path: 'operations/statements', component: BulkStatementsComponent, canActivate: [authGuard, roleGuard], data: { role: 'Operator' } },
  { path: 'operations', component: CustomerWorkspaceComponent, canActivate: [authGuard, roleGuard], data: { role: 'Operator' } },
  { path: 'accounts/:id/transactions/:reference', component: TransactionHistoryDetailComponent, canActivate: [authGuard] },
  { path: 'accounts/:id/transactions', component: TransactionHistoryComponent, canActivate: [authGuard] },
  { path: 'accounts/:id/statements/:generationId', component: StatementComponent, canActivate: [authGuard] },
  { path: 'accounts/:id/statements', component: StatementComponent, canActivate: [authGuard] },
  { path: 'accounts/:id', component: AccountDetailComponent, canActivate: [authGuard] },
  { path: 'administration', component: RoleWorkspaceComponent, canActivate: [authGuard, roleGuard], data: { role: 'Administrator', title: 'Access administration' } },
  { path: '**', component: NotFoundComponent }
];
