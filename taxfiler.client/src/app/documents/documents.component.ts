import { HttpClient } from "@angular/common/http";
import {ActivatedRoute, RouterLink} from "@angular/router";
import { ColDef } from 'ag-grid-community';
import { AllCommunityModule, ModuleRegistry } from 'ag-grid-community';
import { AG_GRID_LOCALE_DE } from '@ag-grid-community/locale';
import { NgIf} from "@angular/common";
import {AgGridAngular} from "ag-grid-angular";
import {Component, OnInit, LOCALE_ID, Inject} from '@angular/core';
import {
  MatDialog, MatDialogTitle
} from '@angular/material/dialog';
import {FormBuilder} from '@angular/forms';
import {MatAnchor} from "@angular/material/button";
import {ButtonCellRendererComponent} from "../button-cell-renderer/button-cell-renderer.component";
import {DocumentEditComponent} from "../document-edit/document-edit.component";

ModuleRegistry.registerModules([AllCommunityModule]);

function formatPrice(value: any):string{
  return value.value ? value.value.toLocaleString('en-US', {
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
    {
      headerName: 'Edit',
      cellRenderer: ButtonCellRendererComponent,
      cellRendererParams: {
        onClickCallback: (data: any) => this.openEditDialog(data)
      },
      editable: false,
      colId: 'params',
      width: 100
    }
  ];

  defaultColDef = {
    flex: 1,
    filter: true,
    cellStyle: {
      textAlign: 'right',
      paddingRight: '30px'
    }
  };
  dialogRef: any;
  openEditDialog(data:any) {
    this.dialogRef =
      this.dialog.open(DocumentEditComponent, {
        width: '50vw',
        maxWidth: '90vw',
        data: data
      });

    this.dialogRef.afterClosed().subscribe(() =>
    {
      this.getDocuments();
    });

  }


  public documents: Document[] = [];
  public yearMonth: any;
  localeText = AG_GRID_LOCALE_DE;
  constructor(private dialog: MatDialog,
              private fb: FormBuilder,
              private http: HttpClient,
              private route: ActivatedRoute,
              @Inject(LOCALE_ID) public locale: string) {
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
