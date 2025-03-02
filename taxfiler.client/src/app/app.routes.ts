import { Routes } from '@angular/router';
import { TransactionsComponent } from './transactions/transactions.component';

export const routes: Routes = [
  { path: '', redirectTo: '/transactions/2025-01', pathMatch: 'full' },
  { path: 'transactions/:yearMonth', component: TransactionsComponent },
];
