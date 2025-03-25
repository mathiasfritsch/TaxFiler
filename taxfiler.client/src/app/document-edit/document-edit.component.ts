import {Component, Inject, OnInit} from "@angular/core";
import {MatFormField, MatLabel} from "@angular/material/form-field";
import {FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from "@angular/material/dialog";
import {MatButton} from "@angular/material/button";
import {MatInput} from "@angular/material/input";
import {Document} from "../model/document";
import {MatCheckbox} from "@angular/material/checkbox";
import {HttpClient} from "@angular/common/http";

function formatPrice(value: any):string{
  return value ?
    value.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      useGrouping: false,
    }) : '';
}

@Component({
  selector: 'dialog-overview-example-dialog',
  templateUrl: './document-edit.component.html',
  styleUrls: ['./document-edit.component.css'],
  standalone: true,
  imports: [
    MatFormField,
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

export class DocumentEditComponent implements OnInit{

  documentFormGroup: FormGroup;

  constructor(
    public dialogRef: MatDialogRef<DocumentEditComponent>,
    @Inject(MAT_DIALOG_DATA) public document: Document,
    private fb: FormBuilder,
    private http: HttpClient
  ) {
    this.documentFormGroup = this.fb.group({
      nameControl: new FormControl(document.name),
      totalControl: new FormControl(formatPrice(document.total)),
      subTotalControl: new FormControl(formatPrice(document.subTotal)),
      taxAmountControl: new FormControl(formatPrice(document.taxAmount)),
      taxRateControl: new FormControl(formatPrice(document.taxRate)),
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
    if (this.documentFormGroup.valid) {

      const updateDocument: Document = {
        id: this.document.id,
        name: this.documentFormGroup.value.nameControl,
        total: this.documentFormGroup.value.totalControl,
        subTotal: this.documentFormGroup.value.subTotalControl,
        taxAmount: this.documentFormGroup.value.taxAmountControl,
        taxRate: this.documentFormGroup.value.taxRateControl,
        skonto: this.documentFormGroup.value.skontoControl==''?null:this.documentFormGroup.value.skontoControl,
        invoiceDate: this.documentFormGroup.value.invoiceDateControl,
        invoiceNumber: this.documentFormGroup.value.invoiceNumberControl,
        parsed: this.documentFormGroup.value.parsedControl
      };

      this.http.post<Document>(`/api/documents/updatedocument`,updateDocument).subscribe(
        {
          next: document => {
            console.log('Success!', document);
            this.dialogRef.close();
          },
          error: error => {
            console.error('There was an error!', error);
          }
        }
      );
    }
  }
  ngOnInit(): void {
  }

}

