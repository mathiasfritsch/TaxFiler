﻿import { Component,Input } from '@angular/core';
import { ICellRendererAngularComp } from 'ag-grid-angular';
import { ICellRendererParams } from 'ag-grid-community';
import { MatDialog } from '@angular/material/dialog';
import {MatButton} from "@angular/material/button";

@Component({
  selector: 'app-button-cell-renderer',
  standalone: true,
  imports: [
    MatButton
  ],
  template: `
    <button mat-button (click)="onClick($event)">{{ buttonText }}</button>`
})
export class ButtonCellRendererComponent implements ICellRendererAngularComp {
  private params: any;
  @Input() onClickCallback: ((data: any) => void) | undefined;
  @Input() buttonText: string | undefined;
  constructor(private dialog: MatDialog) {}

  agInit(params: any): void {
    this.params = params;
    this.onClickCallback = params.onClickCallback;
    this.buttonText = params.buttonText;
  }

  refresh(params: ICellRendererParams): boolean {
    return false;
  }

  onClick($event: any): void {
    if (this.onClickCallback) {
      this.onClickCallback(this.params.data);
    }
  }
}
