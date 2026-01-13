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
import { AsyncPipe } from "@angular/common";
import {MatAutocompleteModule} from '@angular/material/autocomplete';
import { Observable } from "rxjs";
import {map, startWith} from 'rxjs/operators';
import { Account } from "../model/account";
import { DocumentMatch } from "../model/document-match";

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
    MatOption,
    MatCheckbox,
    MatAutocompleteModule,
    AsyncPipe,
    MatSelect
],
  templateUrl: './transaction-edit.component.html',
  standalone: true,
  styleUrl: './transaction-edit.component.css'
})
export class TransactionEditComponent implements OnInit{
  transactionFormGroup: FormGroup;
  documents: Document[] = [];
  filteredDocuments: Observable<Document[]>;
  unconnectedOnly: boolean = true;
  accounts: Account[] = [];

  constructor(
    public dialogRef: MatDialogRef<TransactionEditComponent>,
    @Inject(MAT_DIALOG_DATA) public transaction: Transaction,
    private fb: FormBuilder,
    private http: HttpClient
  ) {

    var document = {
      id: transaction.documentId,
      name: transaction.documentName
    };

    this.transactionFormGroup = this.fb.group({
      transactionNoteControl: new FormControl(transaction.transactionNote),
      netAmountControl: new FormControl(transaction.netAmount),
      grossAmountControl: new FormControl(transaction.grossAmount),
      senderReceiverControl: new FormControl(transaction.senderReceiver),
      documentControl: new FormControl(document),
      taxAmountControl: new FormControl(transaction.taxAmount),
      transactionDateTimeControl: new FormControl(transaction.transactionDateTime),
      isSalesTaxRelevantControl: new FormControl(transaction.isSalesTaxRelevant),
      isIncomeTaxRelevantControl: new FormControl(transaction.isIncomeTaxRelevant),
      accountControl: new FormControl(transaction.accountId)
    });
    this.filteredDocuments = this.transactionFormGroup.controls['documentControl'].valueChanges.pipe(
      map(value => this._filterDocuments(value))
    );
  }
  onCancelClick(): void {
    this.dialogRef.close();
  }
  private _filterDocuments(value: string): Document[] {
    const filterValue = value.toLowerCase();
    const filterValueNumber = parseFloat(value);
    const filterValueDate:Date = new Date(value);

    return this.documents.filter(document =>
      (
        document.name.toLowerCase().includes(filterValue) ||
        document.total == filterValueNumber ||
        document.invoiceDate == filterValueDate
      ) &&
      (
        document.unconnected || !this.unconnectedOnly
      )
    );
  }

  onSaveClick(): void {
    const updatedTransaction = {
      ...this.transaction,
      documentId: this.transactionFormGroup.value.documentControl.id,
      transactionNote: this.transactionFormGroup.value.transactionNoteControl,
      netAmount: this.transactionFormGroup.value.netAmountControl,
      grossAmount: this.transactionFormGroup.value.grossAmountControl,
      senderReceiver: this.transactionFormGroup.value.senderReceiverControl,
      taxAmount: this.transactionFormGroup.value.taxAmountControl,
      transactionDateTime: this.transactionFormGroup.value.transactionDateTimeControl,
      isSalesTaxRelevant: this.transactionFormGroup.value.isSalesTaxRelevantControl,
      isIncomeTaxRelevant: this.transactionFormGroup.value.isIncomeTaxRelevantControl,
      accountId: this.transactionFormGroup.value.accountControl
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
    this.getAccounts();
  }

  displayWithDocumentName(document: Document): string {
    return document && document.name ? document.name : '';
  }

  getDocuments() {
    if (!this.transaction || !this.transaction.id) {
      console.error('Transaction ID is required to fetch document matches');
      return;
    }
    
    this.http.get<DocumentMatch[]>(`/api/documentmatching/matches/${this.transaction.id}`).subscribe(
      {
        next: documentMatches => {
          this.documents = documentMatches.map(match => match.document);
        },
        error: error => {
          console.error('There was an error!', error);
        }
      }
    );
  }

  getAccounts() {
    this.http.get<Account[]>(`/api/accounts/getaccounts`).subscribe({
      next: accounts => {
        this.accounts = accounts;
      },
      error: error => {
        console.error('There was an error fetching accounts!', error);
      }
    });
  }

  changeUnconnectedOnly() {
    this.unconnectedOnly = !this.unconnectedOnly;
  }
}
