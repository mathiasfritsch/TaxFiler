import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Account } from '../model/account';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AgGridModule } from 'ag-grid-angular';
import { ColDef, GridReadyEvent } from 'ag-grid-community';
import { ButtonCellRendererComponent } from '../button-cell-renderer/button-cell-renderer.component';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { NavigationComponent } from '../shared/navigation/navigation.component';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    AgGridModule,
    RouterModule,
    NavigationComponent,
  ],
  templateUrl: './accounts.component.html',
  styleUrls: ['./accounts.component.css']
})
export class AccountsComponent implements OnInit {
  accounts: Account[] = [];
  colDefs: ColDef[] = [];
  yearMonth: string = '';
  defaultColDef: ColDef = {
    sortable: true,
    filter: true,
    resizable: true
  };
  localeText = {
    page: 'Seite',
    more: 'Mehr',
    to: 'bis',
    of: 'von',
    next: 'Nächste',
    previous: 'Vorherige',
    loadingOoo: 'Lädt...',
    noRowsToShow: 'Keine Konten gefunden'
  };

  constructor(
    private http: HttpClient,
    private dialog: MatDialog,
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.yearMonth = params.get('yearMonth') || '';
    });

    this.setupGrid();
    this.getAccounts();
  }

  setupGrid(): void {
    this.colDefs = [
      { field: 'id', headerName: 'ID', width: 80 },
      { field: 'name', headerName: 'Name', flex: 1 }
    ];
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
}
