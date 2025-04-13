import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Account } from '../model/account';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AccountEditComponent } from './account-edit.component';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule
  ],
  templateUrl: './accounts.component.html',
  styleUrls: ['./accounts.component.css']
})
export class AccountsComponent implements OnInit {
  accounts: Account[] = [];
  displayedColumns: string[] = ['id', 'name', 'actions'];

  constructor(
    private http: HttpClient,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.getAccounts();
  }

  getAccounts(): void {
    this.http.get<Account[]>('/api/accounts/getaccounts').subscribe({
      next: (accounts) => {
        this.accounts = accounts;
      },
      error: (error) => {
        console.error('Error fetching accounts:', error);
      }
    });
  }

  openAccountDialog(account?: Account): void {
    const dialogRef = this.dialog.open(AccountEditComponent, {
      width: '400px',
      data: account || { id: 0, name: '' }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.getAccounts();
      }
    });
  }

  deleteAccount(id: number): void {
    if (confirm('Are you sure you want to delete this account?')) {
      this.http.delete(`/api/accounts/deleteaccount/${id}`).subscribe({
        next: () => {
          this.getAccounts();
        },
        error: (error) => {
          console.error('Error deleting account:', error);
        }
      });
    }
  }
} 