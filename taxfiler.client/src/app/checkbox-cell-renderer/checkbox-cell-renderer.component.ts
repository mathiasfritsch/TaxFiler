import { Component } from '@angular/core';
import { ICellRendererAngularComp } from 'ag-grid-angular';
import { ICellRendererParams } from 'ag-grid-community';
import { MatCheckbox } from "@angular/material/checkbox";
import { FormsModule } from "@angular/forms";

@Component({
  selector: 'app-checkbox-cell-renderer',
  standalone: true,
  imports: [
    MatCheckbox,
    FormsModule
  ],
  template: `
    <mat-checkbox 
      [(ngModel)]="checked" 
      (change)="onCheckboxChange($event)">
    </mat-checkbox>`
})
export class CheckboxCellRendererComponent implements ICellRendererAngularComp {
  public params!: ICellRendererParams;
  public checked: boolean = false;

  agInit(params: ICellRendererParams): void {
    this.params = params;
    this.checked = params.value === true;
  }

  refresh(params: ICellRendererParams): boolean {
    this.params = params;
    this.checked = params.value === true;
    return true;
  }

  onCheckboxChange(event: any): void {
    // Update the cell value in the grid if setValue is available
    if (this.params.setValue) {
      this.params.setValue(this.checked);
    }
    
    // Trigger the onCellValueChanged event if callback is provided
    if (this.params.node && this.params.api) {
      const rowNode = this.params.node;
      const colDef = this.params.colDef;
      
      // Manually trigger the cell value changed event
      this.params.api.dispatchEvent({
        type: 'cellValueChanged',
        node: rowNode,
        data: rowNode.data,
        oldValue: !this.checked,
        newValue: this.checked,
        colDef: colDef,
        column: this.params.column,
        api: this.params.api,
        columnApi: null,
        context: this.params.context
      } as any);
    }
  }
}
