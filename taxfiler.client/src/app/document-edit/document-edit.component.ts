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
import {JsonPipe} from "@angular/common";

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
      totalControl: new FormControl(document.total),
      subTotalControl: new FormControl(document.subTotal),
    });
    console.log(document)
    // this.editDocumentForm = this.fb.group({
    //   id: [document.id],
    //   total: [document.total],
    //   subTotal: [document.subTotal],
    //   taxAmount: [document.taxAmount],
    //   skonto: [document.skonto],
    //   invoiceDate: [document.invoiceDate],
    //   invoiceNumber: [document.invoiceNumber],
    //   parsed: [document.parsed]
    // });
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

