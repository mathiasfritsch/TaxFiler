import { HttpClient } from "@angular/common/http";
import {ActivatedRoute, Router, RouterLink} from "@angular/router";
import { ColDef } from 'ag-grid-community';
import { AllCommunityModule, ModuleRegistry,RowValueChangedEvent } from 'ag-grid-community';
import { AG_GRID_LOCALE_DE } from '@ag-grid-community/locale';
import { NgIf} from "@angular/common";
import {AgGridAngular} from "ag-grid-angular";
import {Component, OnInit, LOCALE_ID, Inject} from '@angular/core';
import {
  MatDialog, MatDialogTitle
} from '@angular/material/dialog';
import {FormBuilder} from '@angular/forms';
import {DialogOverviewExampleDialog} from "../document-edit/document-edit.component";
import {MatAnchor, MatButton} from "@angular/material/button";

ModuleRegistry.registerModules([AllCommunityModule]);

function formatPrice(value: any):string{
  return value.value ? value.value.toLocaleString('de-DE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }) : '';
}

@Component({
  selector: 'app-documents',
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.css'],
  imports: [
    RouterLink,
    NgIf,
    AgGridAngular,
    MatButton,
    MatDialogTitle,
    MatAnchor,
  ],
  standalone: true
})

export class DocumentsComponent implements  OnInit{



  colDefs: ColDef[] = [
    {
      field: 'name',
      headerName: 'Name',
      filter: true,
      cellStyle: {
        textAlign: 'left'
      }
    },
    {
      field: 'total' ,
      headerName: 'Brutto',
      valueFormatter: formatPrice,
    },
    {
      field: 'subTotal',
      headerName:'Netto',
      valueFormatter: formatPrice,
    },
    {
      field: 'taxAmount',
      headerName:'Steuerbetrag',
      valueFormatter: formatPrice,
    },
    {
      field: 'skonto' ,
      headerName:'Skonto',
      valueFormatter: formatPrice,
    },
    { field: 'invoiceDate', headerName:'Rechungsdatum', filter: true },
    {
      field: 'invoiceNumber' ,
      headerName:'Rechnungsnummer',
      cellStyle: { textAlign: 'left' }
    },
    { field: 'parsed', headerName:'Parsed', filter: true},
  ];

  defaultColDef = {
    flex: 1,
    filter: true,
    cellStyle: {
      textAlign: 'right',
      paddingRight: '30px'
    }
  };

  onRowValueChanged(event: RowValueChangedEvent) {
    const data = event.data;
    console.log(data);

  }
  public today:Date = new Date();
  public documents: Document[] = [];
  public yearMonth: any;
  public amount: number = 10.24;
  localeText = AG_GRID_LOCALE_DE;

  constructor(private dialog: MatDialog,
              private fb: FormBuilder,
              private http: HttpClient,
              private route: ActivatedRoute,
              @Inject(LOCALE_ID) public locale: string) {
  }

  openEditDocument(): void {
    const dialogRef = this.dialog.open(DialogOverviewExampleDialog, {
      width: '600px',
      data: this.documents[0]
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('The dialog was closed');
    });
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.yearMonth = params.get('yearMonth');
      this.getDocuments();
    });

  }
  getDocuments() {
    this.http.get<any[]>(`/api/documents/getdocuments`).subscribe(
      {
        next: documents => {
          this.documents = documents;
        },
        error: error => {
          console.error('There was an error!', error);
        }
      }
    );
  }
}
