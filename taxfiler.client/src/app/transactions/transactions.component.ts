import {Component, OnInit} from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { AllCommunityModule, ModuleRegistry } from 'ag-grid-community';
import {NgIf} from "@angular/common";
import {AgGridAngular} from "ag-grid-angular";
import {MatDialogTitle} from "@angular/material/dialog";
import {MatAnchor} from "@angular/material/button";
import {AG_GRID_LOCALE_DE} from "@ag-grid-community/locale";

ModuleRegistry.registerModules([AllCommunityModule]);

function formatPrice(value: any):string{
  return value.value ? value.value.toLocaleString('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }) : '';
}

@Component({
    selector: 'app-transactions',
    templateUrl: './transactions.component.html',
    styleUrls: ['./transactions.component.css'],
    standalone: true,
    imports: [
      RouterLink,
      NgIf,
      AgGridAngular,
      MatDialogTitle,
      MatAnchor,
    ]
})
export class TransactionsComponent  implements  OnInit{
  public transactions: any[] = [];
  public yearMonth: any;
  localeText = AG_GRID_LOCALE_DE;
  colDefs: ColDef[] = [
    {
      field: 'id',
      headerName: 'id',
      filter: false,
    },
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
      field: 'documentName',
      headerName: 'Dokument',
    },
    {
      field: 'taxAmount',
      headerName: 'Steuer',
    },
    {
      field: 'transactionDateTime',
      headerName: 'Datum',
    },
    {
      field: 'taxAmount',
      headerName: 'Steuer',
    },
    {
      field: 'isSalesTaxRelevant',
      headerName: 'Umsatzsteuerrelevant',
    },
  ];

  defaultColDef = {
    flex: 1,
    filter: true,
    cellStyle: {
      textAlign: 'right',
      paddingRight: '30px'
    }
  };

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router) {}
  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.yearMonth = params.get('yearMonth');
      this.getTransactions(this.yearMonth);
    });
  }

  getTransactions(yearMonth: any) {
    console.log(yearMonth);
    this.http.get<any[]>(`/api/transactions/gettransactions?yearMonth=${yearMonth}`).subscribe(
      {
        next: transactions => {
          this.transactions = transactions;
        },
        error: error => {
          console.error('There was an error!', error);
        }
      }
    );
  }

  switchMonth(offset: number) {
    const [year, month] = this.yearMonth.split('-').map(Number);
    const date = new Date(year, month - 1 + offset, 1);
    const newYearMonth = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, '0')}`;
    this.router.navigate([`/transactions/${newYearMonth}`]).then();
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
}
