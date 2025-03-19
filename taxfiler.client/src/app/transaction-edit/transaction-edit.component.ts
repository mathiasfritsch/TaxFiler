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
import {Transaction} from "../model/transaction";
import {MatOption, MatSelect} from "@angular/material/select";
import {NgForOf} from "@angular/common";

@Component({
  selector: 'app-transaction-edit',
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
    MatSelect,
    MatOption,
    NgForOf,
    MatCheckbox,
  ],
  templateUrl: './transaction-edit.component.html',
  standalone: true,
  styleUrl: './transaction-edit.component.css'
})
export class TransactionEditComponent implements OnInit{
  transactionFormGroup: FormGroup;
  documents: any[] = [];

  constructor(
    public dialogRef: MatDialogRef<TransactionEditComponent>,
    @Inject(MAT_DIALOG_DATA) public transaction: Transaction,
    private fb: FormBuilder,
    private http: HttpClient
  ) {
    this.transactionFormGroup = this.fb.group({
      transactionNoteControl: new FormControl(transaction.transactionNote),
      netAmountControl: new FormControl(transaction.netAmount),
      grossAmountControl: new FormControl(transaction.grossAmount),
      senderReceiverControl: new FormControl(transaction.senderReceiver),
      documentControl: new FormControl(transaction.documentId),
      taxAmountControl: new FormControl(transaction.taxAmount),
      transactionDateTimeControl: new FormControl(transaction.transactionDateTime),
      isSalesTaxRelevantControl: new FormControl(transaction.isSalesTaxRelevant),
    });
  }
  onCancelClick(): void {
    this.dialogRef.close();
  }
  onSaveClick(): void {
    const updatedTransaction = {
      ...this.transaction,
      documentId: this.transactionFormGroup.value.documentControl,
      transactionNote: this.transactionFormGroup.value.transactionNoteControl,
      netAmount: this.transactionFormGroup.value.netAmountControl,
      grossAmount: this.transactionFormGroup.value.grossAmountControl,
      senderReceiver: this.transactionFormGroup.value.senderReceiverControl,
      taxAmount: this.transactionFormGroup.value.taxAmountControl,
      transactionDateTime: this.transactionFormGroup.value.transactionDateTimeControl,
      isSalesTaxRelevant: this.transactionFormGroup.value.isSalesTaxRelevantControl,
    };

    this.http.post('/api/transactions/updatetransaction', updatedTransaction).subscribe({
      next: () => {
        this.dialogRef.close(updatedTransaction);
      },
      error: error => {
        console.error('There was an error!', error);
      }
    });

    this.dialogRef.close();
  }
  ngOnInit(): void {
    this.getDocuments();
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
