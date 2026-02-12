import { Routes } from '@angular/router';
import { TransactionsComponent } from './transactions/transactions.component';
import { DocumentsComponent } from "./documents/documents.component";
import { AccountsComponent } from './accounts/accounts.component';
import { MigrationsComponent } from './migrations/migrations.component';

function getStartOfMonth(): string {
  const date = new Date();
  const firstDay = new Date(date.getFullYear(), date.getMonth(), 1);
  const year = firstDay.getFullYear();
  const month = (firstDay.getMonth() + 1).toString().padStart(2, '0');
  return `${year}-${month}`;
}

export const routes: Routes = [
  {
    path: '',
    redirectTo: `/transactions/${getStartOfMonth()}`,
    pathMatch: 'full'
  },
  {
    path: 'transactions/:yearMonth',
    component: TransactionsComponent
  },
  {
    path: 'documents/:yearMonth',
    component: DocumentsComponent
  },
  {
    path: 'documents/:yearMonth/:documentId',
    component: DocumentsComponent
  },
  {
    path: 'accounts',
    redirectTo: `accounts/${getStartOfMonth()}`,
    pathMatch: 'full'
  },
  {
    path: 'accounts/:yearMonth',
    component: AccountsComponent
  },
  {
    path: 'migrations',
    component: MigrationsComponent
  },
];
