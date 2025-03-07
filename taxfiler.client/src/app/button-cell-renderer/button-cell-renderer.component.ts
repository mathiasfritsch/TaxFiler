import { Component } from '@angular/core';
import { ICellRendererAngularComp } from 'ag-grid-angular';
import { ICellRendererParams } from 'ag-grid-community';
import { MatDialog } from '@angular/material/dialog';
import { DialogOverviewExampleDialog } from '../document-edit/document-edit.component';
import {MatButton} from "@angular/material/button";

@Component({
  selector: 'app-button-cell-renderer',
  standalone: true,
  imports: [
    MatButton
  ],
  template: `
    <button mat-button (click)="onClick($event)">Edit</button>`
})
export class ButtonCellRendererComponent implements ICellRendererAngularComp {
  private params: any;

  constructor(private dialog: MatDialog) {}

  agInit(params: ICellRendererParams): void {
    this.params = params;
  }

  refresh(params: ICellRendererParams): boolean {
    return false;
  }

  onClick($event: any): void {
    this.dialog.open(DialogOverviewExampleDialog, {
      width: '600px',
      data: this.params.data
    });
  }
}
