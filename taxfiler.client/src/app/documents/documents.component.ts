import { HttpClient } from "@angular/common/http";
import {ActivatedRoute, Router, RouterLink} from "@angular/router";
import { ColDef } from 'ag-grid-community';
import { AllCommunityModule, ModuleRegistry,RowValueChangedEvent } from 'ag-grid-community';
import {NgForOf, NgIf} from "@angular/common";
import {AgGridAngular} from "ag-grid-angular";
import {Component, OnInit} from '@angular/core';
import {
  MatDialog, MatDialogTitle
} from '@angular/material/dialog';
import {FormBuilder, FormGroup} from '@angular/forms';
import {DialogOverviewExampleDialog} from "../document-edit/document-edit.component";
import {MatAnchor, MatButton} from "@angular/material/button";

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-documents',
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.css'],
  imports: [
    RouterLink,
    NgIf,
    NgForOf,
    AgGridAngular,
    MatButton,
    MatDialogTitle,
    MatAnchor
  ],
  standalone: true
})

export class DocumentsComponent implements  OnInit{
  editForm: FormGroup;

  animal: string = '';
  name: string = '';

  colDefs: ColDef[] = [
    { field: 'name', headerName: 'Name', filter: true},
    { field: 'total' , headerName: 'Brutto', filter: true},
    { field: 'subTotal', headerName:'Netto', filter: true },
    { field: 'taxAmount', headerName:'Steuerbetrag', filter: true },
    { field: 'skonto' , headerName:'Skonto', filter: true},
    { field: 'invoiceDate', headerName:'Rechungsdatum', filter: true },
    { field: 'invoiceNumber' , headerName:'Rechnungsnummer', filter: true},
    { field: 'parsed', headerName:'Parsed', filter: true},
  ];

  defaultColDef = {
    flex: 1,
    editable: true,
  };

  onRowValueChanged(event: RowValueChangedEvent) {
    const data = event.data;
    console.log(data);

  }


  public documents: any[] = [];
  public yearMonth: any;

  constructor(private dialog: MatDialog,
              private fb: FormBuilder,
              private http: HttpClient,
              private route: ActivatedRoute,
              private router: Router) {

    this.editForm = this.fb.group({
      documentName: ['']
    });

  }
  openEditDocument(): void {
    const dialogRef = this.dialog.open(DialogOverviewExampleDialog, {
      width: '250px',
      data: {name: this.name, animal: this.animal}
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('The dialog was closed');
      this.animal = result;
    });
  }

  onSave(): void {
    const documentId = this.dialog.getDialogById('id')?.componentInstance.data.id;
    this.http.post(`/api/documents/updatedocument`, this.editForm.value).subscribe(() => {
      this.dialog.closeAll();
      this.getDocuments();
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
