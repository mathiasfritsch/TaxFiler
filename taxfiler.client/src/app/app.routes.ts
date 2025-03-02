import { Routes } from '@angular/router';
import { TransactionsComponent } from './transactions/transactions.component';

function getStartOfMonth(): string {
  const date = new Date();
  const firstDay = new Date(date.getFullYear(), date.getMonth(), 1);
  const year = firstDay.getFullYear();
  const month = (firstDay.getMonth() + 1).toString().padStart(2, '0');
  const day = firstDay.getDate().toString().padStart(2, '0');
  return `${year}-${month}-${day}`;
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
];
