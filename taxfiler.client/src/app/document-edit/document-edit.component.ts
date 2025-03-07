import {Component, Inject, OnInit} from "@angular/core";
import {MatFormField, MatLabel} from "@angular/material/form-field";
import {FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from "@angular/material/dialog";
import {MatButton} from "@angular/material/button";
import {MatInput} from "@angular/material/input";
import {Document} from "../model/document";
import {MatCheckbox} from "@angular/material/checkbox";

function formatPrice(value: any):string{
  return value ?
    value.toLocaleString('de-DE', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }) : '';
}

@Component({
  selector: 'dialog-overview-example-dialog',
  templateUrl: './document-edit.component.html',
  styleUrls: ['./document-edit.component.css'],
  standalone: true,
  imports: [
    MatFormField,
    MatDialogClose,
    MatDialogActions,
    MatButton,
    MatDialogContent,
    MatDialogTitle,
    ReactiveFormsModule,
    FormsModule,
    MatInput,
    MatLabel,
    MatCheckbox
  ]
})

export class DialogOverviewExampleDialog implements OnInit{

  documentFormGroup: FormGroup;

  constructor(
    public dialogRef: MatDialogRef<DialogOverviewExampleDialog>,
    @Inject(MAT_DIALOG_DATA) public document: Document,
    private fb: FormBuilder
  ) {
    this.documentFormGroup = this.fb.group({
      nameControl: new FormControl(document.name),
      totalControl: new FormControl(formatPrice(document.total)),
      subTotalControl: new FormControl(formatPrice(document.subTotal)),
      taxAmountControl: new FormControl(formatPrice(document.taxAmount)),
      skontoControl: new FormControl(formatPrice(document.skonto)),
      invoiceNumberControl: new FormControl(formatPrice(document.invoiceNumber)),
      parsedControl: new FormControl(document.parsed),
      invoiceDateControl: new FormControl(document.invoiceDate)
    });
  }

  onCancelClick(): void {
    this.dialogRef.close();
  }
  onSaveClick(): void {

  }
  ngOnInit(): void {
    this.documentFormGroup.value.nameControl = 'asfd'
  }

}

