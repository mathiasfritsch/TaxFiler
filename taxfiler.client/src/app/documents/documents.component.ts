import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { AllCommunityModule, ModuleRegistry } from 'ag-grid-community';
import { AG_GRID_LOCALE_DE } from '@ag-grid-community/locale';
import { CommonModule, NgIf } from '@angular/common';
import { AgGridAngular } from 'ag-grid-angular';
import { Component, OnInit, LOCALE_ID, Inject } from '@angular/core';
import { MatDialog, MatDialogTitle } from '@angular/material/dialog';
import { FormBuilder } from '@angular/forms';
import { MatAnchor, MatButton } from '@angular/material/button';
import { ButtonCellRendererComponent } from '../button-cell-renderer/button-cell-renderer.component';
import { DocumentEditComponent } from '../document-edit/document-edit.component';
import { Document } from '../model/document';
import { Observable } from 'rxjs';

ModuleRegistry.registerModules([AllCommunityModule]);

function formatPrice(value: any): string {
  return value.value
    ? value.value.toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      })
    : '';
}

@Component({
  selector: 'app-documents',
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.css'],
  imports: [
    RouterLink,
    AgGridAngular,
    MatDialogTitle,
    MatAnchor,
    MatButton,
    CommonModule,
  ],
  standalone: true,
})
export class DocumentsComponent implements OnInit {
  colDefs: ColDef[] = [
    {
      field: 'name',
      headerName: 'Name',
      filter: true,
      cellStyle: {
        textAlign: 'left',
      },
    },
    {
      field: 'total',
      headerName: 'Brutto',
      valueFormatter: formatPrice,
    },
    {
      field: 'subTotal',
      headerName: 'Netto',
      valueFormatter: formatPrice,
    },
    {
      field: 'taxAmount',
      headerName: 'Steuerbetrag',
      valueFormatter: formatPrice,
    },
    {
      field: 'taxRate',
      headerName: 'Steuersatz',
      valueFormatter: formatPrice,
    },
    {
      field: 'skonto',
      headerName: 'Skonto',
      valueFormatter: formatPrice,
    },
    { field: 'invoiceDate', headerName: 'Rechungsdatum', filter: true },
    {
      field: 'invoiceNumber',
      headerName: 'Rechnungsnummer',
      cellStyle: { textAlign: 'left' },
    },
    { field: 'parsed', headerName: 'Parsed', filter: true },
    { field: 'unconnected', headerName: 'Unconnected', filter: true },
    {
      headerName: 'Edit',
      cellRenderer: ButtonCellRendererComponent,
      cellRendererParams: {
        onClickCallback: (data: any) => this.openEditDialog(data),
        buttonText: 'Edit',
        enabled: true,
      },
      editable: false,
      colId: 'params',
      maxWidth: 150,
    },
    {
      headerName: 'Parse',
      cellRenderer: ButtonCellRendererComponent,
      cellRendererParams: {
        onClickCallback: (data: any, button: any) =>
          this.parseDocument(data, button),
        buttonText: 'Parse',
        enabled: true,
      },
      editable: false,
      colId: 'params',
      width: 150,
    },
    {
      headerName: 'Delete',
      cellRenderer: ButtonCellRendererComponent,
      cellRendererParams: {
        onClickCallback: (data: any, button: any) =>
          this.deleteDocument(data, button),
        buttonText: 'Delete',
        enabled: true,
      },
      editable: false,
      colId: 'params',
      width: 150,
    },
  ];

  defaultColDef = {
    flex: 1,
    filter: true,
    cellStyle: {
      textAlign: 'right',
      paddingRight: '30px',
    },
  };
  dialogRef: any;
  openEditDialog(data: any) {
    this.dialogRef = this.dialog.open(DocumentEditComponent, {
      width: '50vw',
      maxWidth: '90vw',
      data: data,
    });

    this.dialogRef.afterClosed().subscribe(() => {
      this.getDocuments();
    });
  }

  documents$: Observable<Document[]> | undefined;

  public yearMonth: any;
  localeText = AG_GRID_LOCALE_DE;
  constructor(
    private dialog: MatDialog,
    private fb: FormBuilder,
    private http: HttpClient,
    private route: ActivatedRoute,
    @Inject(LOCALE_ID) public locale: string
  ) {}

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      this.yearMonth = params.get('yearMonth');
      this.getDocuments();
    });
  }

  getDocuments() {
    this.documents$ = this.http.get<Document[]>(`/api/documents/getdocuments`);
  }

  private parseDocument(
    document: Document,
    button: ButtonCellRendererComponent
  ) {
    button.enabled = false;
    this.http.post<any>(`/api/documents/parse/${document.id}`, {}).subscribe({
      next: (documents) => {
        button.enabled = true;
      },
      error: (error) => {
        button.enabled = true;
        alert('There was an error parsing the document');
      },
    });
  }

  synchronize() {
    this.http.post(`/api/documents/syncfiles/${this.yearMonth}`, {}).subscribe({
      next: (documents) => {
        this.getDocuments();
      },
      error: (error) => {
        console.error('There was an error!', error);
      },
    });
  }

  private deleteDocument(document: Document, button: any) {
    button.enabled = false;
    this.http
      .delete<any>(`/api/documents/deleteDocument/${document.id}`, {})
      .subscribe({
        next: (documents) => {
          button.enabled = true;
        },
        error: (error) => {
          button.enabled = true;
          alert('There was an error deleting the document');
        },
      });
  }
}
