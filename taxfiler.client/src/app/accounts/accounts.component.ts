import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { Account } from '../model/account';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { AgGridAngular } from 'ag-grid-angular';
import { ColDef, AllCommunityModule, ModuleRegistry } from 'ag-grid-community';
import { AG_GRID_LOCALE_DE } from '@ag-grid-community/locale';
import { NavigationComponent } from '../shared/navigation/navigation.component';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [
    AgGridAngular,
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    NavigationComponent
  ],
  templateUrl: './accounts.component.html',
  styleUrls: ['./accounts.component.css']
})
export class AccountsComponent implements OnInit {
  accounts$: Observable<Account[]> | undefined;
  colDefs: ColDef[] = [];
  yearMonth: string = '';
  defaultColDef: ColDef = {
    flex: 1,
    sortable: true,
    filter: true,
    resizable: true
  };
  localeText = AG_GRID_LOCALE_DE;

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute
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
    this.accounts$ = this.http.get<Account[]>('/api/accounts/getaccounts');
  }
}
