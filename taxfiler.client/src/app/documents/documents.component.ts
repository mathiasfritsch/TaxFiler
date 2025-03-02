import {Component, OnInit} from '@angular/core';
import { HttpClient } from "@angular/common/http";
import {ActivatedRoute, Router, RouterLink} from "@angular/router";
import { ColDef } from 'ag-grid-community';
import { AllCommunityModule, ModuleRegistry } from 'ag-grid-community';
import {NgForOf, NgIf} from "@angular/common";
import {AgGridAngular} from "ag-grid-angular";

ModuleRegistry.registerModules([AllCommunityModule]);
@Component({
  selector: 'app-documents',
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.css'],
  imports: [
    RouterLink,
    NgIf,
    NgForOf,
    AgGridAngular
  ],
  standalone: true
})

export class DocumentsComponent implements  OnInit{

  colDefs: ColDef[] = [
    { field: 'name', headerName: 'Name', filter: true },
    { field: 'total' , headerName: 'Brutto', filter: true },
    { field: 'subTotal', headerName:'Netto', filter: true },
    { field: 'taxAmount', headerName:'Steuerbetrag', filter: true },
    { field: 'skonto' , headerName:'Skonto', filter: true},
    { field: 'invoiceDate', headerName:'Rechungsdatum', filter: true },
    { field: 'invoiceNumber' , headerName:'Rechnungsnummer', filter: true},
    { field: 'parsed', headerName:'Parsed', filter: true},
  ];

  defaultColDef = {
    flex: 1,
  };

  public documents: any[] = [];
  public yearMonth: any;

  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router) {}

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
