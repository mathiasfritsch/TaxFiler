import {Component, OnInit} from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import {ActivatedRoute, Router} from '@angular/router';
import { ColDef, CellValueChangedEvent } from 'ag-grid-community';
import { AllCommunityModule, ModuleRegistry } from 'ag-grid-community';
import { combineLatest, Observable, firstValueFrom } from 'rxjs';
import { CommonModule, AsyncPipe } from '@angular/common';

import {AgGridAngular} from "ag-grid-angular";
import {MatDialog, MatDialogTitle} from "@angular/material/dialog";
import {MatButton} from "@angular/material/button";
//import {MatIcon} from "@angular/material/icon";
//import {MatProgressSpinner} from "@angular/material/progress-spinner";
import {MatTooltip} from "@angular/material/tooltip";

import {AG_GRID_LOCALE_DE} from "@ag-grid-community/locale";
import {ButtonCellRendererComponent} from "../button-cell-renderer/button-cell-renderer.component";
import {TransactionEditComponent} from "../transaction-edit/transaction-edit.component";
import {Transaction} from "../model/transaction";
import {NavigationComponent} from '../shared/navigation/navigation.component';
import {AutoAssignResult} from "../model/auto-assign-result";
import {map} from 'rxjs/operators';
import {AutoAssignResultDialogComponent} from "../auto-assign-result-dialog/auto-assign-result-dialog.component";

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css'],
  standalone: true,
  imports: [
    AgGridAngular,
    MatDialogTitle,
    NavigationComponent,
    CommonModule,
    AsyncPipe,
    MatButton,
    //MatIcon,
    //MatProgressSpinner,
    MatTooltip
  ]
})
export class TransactionsComponent  implements  OnInit{
  public transactions$: Observable<any[]> | undefined;
  public yearMonth: any;
  public accountId: number | null = null;
  localeText = AG_GRID_LOCALE_DE;

  // Auto-assign properties
  isAutoAssigning = false;

  colDefs: ColDef[] = [
    {
      field: 'netAmount',
      headerName: 'Netto',
    },
    {
      field: 'grossAmount',
      headerName: 'Brutto',
    },
    {
      field: 'senderReceiver',
      headerName: 'Sender/Empfänger',
    },
    {
      field: 'transactionNote',
      headerName: 'Kommentar',
    },
    {
      field: 'accountName',
      headerName: 'Konto',
    },
    {
      field: 'documentName',
      headerName: 'Dokument',
    },
    {
      field: 'taxAmount',
      headerName: 'Steuer',
    },
    {
      field: 'isTaxMismatch',
      headerName: 'Steuerfehler',
      cellRenderer: (params: any) => {
        if (params.value === true) {
          return '⚠️';
        }
        return '';
      },
      cellStyle: (params: any) => {
        if (params.value === true) {
          return {
            backgroundColor: '#ffebee',
            color: '#c62828',
            textAlign: 'center',
            fontSize: '20px',
            cursor: 'pointer'
          };
        }
        return null;
      },
      onCellClicked: (params: CellValueChangedEvent) => {
        if (params.value === true) {
          // Confirm the tax mismatch - send only the updated transaction
          const updatedTransaction = {
            ...params.data,
            isTaxMismatchConfirmed: true
          };
          // Update the transaction on the server
          this.http.post('/api/transactions/updateTransaction', updatedTransaction)
            .subscribe({
              next: () => {
                console.log('Tax mismatch confirmed successfully');
                // Refresh the grid to show updated data
                this.getTransactions(this.yearMonth);
              },
              error: error => {
                console.error('Error confirming tax mismatch:', error);
                // Refresh the grid to revert changes on error
                this.getTransactions(this.yearMonth);
              }
            });
        }
      },
      maxWidth: 120
    },
    {
      field: 'transactionDateTime',
      headerName: 'Datum',
    },
    {
      field: 'isSalesTaxRelevant',
      headerName: 'Umsatzsteuerrelevant',
      editable: true,
      cellEditor: 'agCheckboxCellEditor',
    },
    {
      field: 'isIncomeTaxRelevant',
      headerName: 'Einkommenssteuerrelevant',
      editable: true,
      cellEditor: 'agCheckboxCellEditor',
    },
    {
      headerName: 'Edit',
      cellRenderer: ButtonCellRendererComponent,
      cellRendererParams: {
        onClickCallback: (data: any) => this.openEditDialog(data),
        buttonText: 'Edit',
        enabled:true,
        dataId: 'button-edit'
      },
      editable: false,
      colId: 'params',
      maxWidth: 150
    },
    {
      headerName: 'Delete',
      cellRenderer: ButtonCellRendererComponent,
      cellRendererParams: {
        onClickCallback: (data: any, button:any) => this.deleteTransaction(data, button),
        buttonText: 'Delete',
        enabled:true,
        dataId: 'button-delete'
      },
      editable: false,
      colId: 'params',
      maxWidth: 150
    }
  ];
  dialogRef: any;
  openEditDialog(data:any) {
    this.dialogRef =
      this.dialog.open(TransactionEditComponent, {
        width: '50vw',
        maxWidth: '90vw',
        data: data
      });

    this.dialogRef.afterClosed().subscribe(() =>
    {
      this.getTransactions(this.yearMonth);
    });

  }
  defaultColDef = {
    flex: 1,
    filter: true,
    cellStyle: {
      textAlign: 'right',
      paddingRight: '30px'
    }
  };

  constructor(private http: HttpClient,
              private route: ActivatedRoute,
              private router: Router,
              private dialog: MatDialog) {}

  ngOnInit() {
    console.log('TransactionsComponent ngOnInit called');
    combineLatest([
      this.route.paramMap,
      this.route.queryParams
    ]).subscribe(([params, queryParams]) => {
      this.yearMonth = params.get('yearMonth');
      this.accountId = queryParams['accountId'] ? parseInt(queryParams['accountId']) : null;
      console.log('Route params changed:', { yearMonth: this.yearMonth, accountId: this.accountId });
      if (this.yearMonth) {
        this.getTransactions(this.yearMonth);
      }
    });
  }

  getTransactions(yearMonth: any) {
    console.log('getTransactions called with:', yearMonth);
    let url = `/api/transactions/gettransactions?yearMonth=${yearMonth}`;
    if (this.accountId) {
      url += `&accountId=${this.accountId}`;
    }
    this.transactions$ = this.http.get<any[]>(url);
  }

  switchMonth(offset: number) {
    const [year, month] = this.yearMonth.split('-').map(Number);
    const date = new Date(year, month - 1 + offset, 1);
    const newYearMonth = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, '0')}`;
    const queryParams = this.accountId ? { accountId: this.accountId } : {};
    this.router.navigate([`/transactions/${newYearMonth}`], { queryParams }).then();
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      const formData = new FormData();
      formData.append('file', file);

      const headers = new HttpHeaders();
      headers.append('Content-Type', 'multipart/form-data');

      this.http.post('/api/transactions/upload?', formData, { headers })
        .subscribe(
          {
            next: () => {
              this.getTransactions(this.yearMonth);
            },
            error: error => {
              console.error('There was an error!', error);
            }
          }
        );
    }
  }

  private deleteTransaction(transaction: Transaction, button: any) {
    this.http.delete(`/api/transactions/deleteTransaction/${transaction.id}`)
      .subscribe(
        {
          next: () => {
            button.enabled = true;
          }
        }
      );
  }

  downloadReport() {
    let url = `/api/transactions/download?yearMonth=${this.yearMonth}`;
    if (this.accountId) {
      url += `&accountId=${this.accountId}`;
    }
    window.location.href = url;
  }

  onCellValueChanged(event: CellValueChangedEvent) {
    const transaction = event.data;
    const updatedTransaction = {
      ...transaction
    };

    this.http.post('/api/transactions/updateTransaction', updatedTransaction)
      .subscribe({
        next: () => {
          console.log('Transaction updated successfully');
        },
        error: error => {
          console.error('Error updating transaction:', error);
          // Refresh the grid to revert changes on error
          this.getTransactions(this.yearMonth);
        }
      });
  }

  async autoAssignDocuments(): Promise<void> {
    if (this.isAutoAssigning) {
      return;
    }

    this.isAutoAssigning = true;

    try {
      const url = `/api/transactions/auto-assign?yearMonth=${this.yearMonth}`;

      const result = await firstValueFrom(this.http.post<AutoAssignResult>(url, {}));

      // Open the Material Dialog to show results
      if (result) {
        this.dialog.open(AutoAssignResultDialogComponent, {
          width: '500px',
          data: result
        });
      }

      // Refresh the grid to show updated assignments
      this.getTransactions(this.yearMonth);
    } catch (error) {
      console.error('Error during auto-assignment:', error);
      alert('An error occurred during auto-assignment. Please try again.');
    } finally {
      this.isAutoAssigning = false;
    }
  }

  getAutoAssignTooltip(): string {
    if (this.isAutoAssigning) {
      return 'Auto-assignment in progress...';
    }
    // Note: This is a synchronous method, but hasUnmatchedTransactions is async
    // For the tooltip, we'll provide a generic message
    return 'Automatically assign documents to unmatched transactions';
  }
}
