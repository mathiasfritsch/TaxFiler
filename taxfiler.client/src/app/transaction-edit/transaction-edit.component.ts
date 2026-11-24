import {Component, Inject, OnInit, ChangeDetectionStrategy} from "@angular/core";
import {MatFormField, MatLabel} from "@angular/material/form-field";
import {FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from "@angular/material/dialog";
import {MatButton, MatIconButton} from "@angular/material/button";
import {MatTooltip} from "@angular/material/tooltip";
import {MatIcon} from "@angular/material/icon";
import {MatInput} from "@angular/material/input";
import {Document} from "../model/document";
import {MatCheckbox} from "@angular/material/checkbox";
import {HttpClient} from "@angular/common/http";
import {Transaction, TransactionDocumentLink} from "../model/transaction";
import {MatOption, MatSelect} from "@angular/material/select";
import { AsyncPipe, CommonModule } from "@angular/common";
import {MatAutocompleteModule} from '@angular/material/autocomplete';
import { Observable } from "rxjs";
import {map, startWith} from 'rxjs/operators';
import { Account } from "../model/account";
import { Router } from "@angular/router";

interface LinkedDocument {
  documentId: number;
  name: string;
  sequence: number;
}

@Component({
  selector: 'app-transaction-edit',
  imports: [
    MatFormField,
    MatDialogActions,
    MatButton,
    MatIconButton,
    MatTooltip,
    MatIcon,
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
    MatSelect,
    CommonModule
],
  templateUrl: './transaction-edit.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './transaction-edit.component.css'
})
export class TransactionEditComponent implements OnInit{
  transactionFormGroup: FormGroup;
  documentSearchControl = new FormControl<string | Document>('');
  availableDocuments: Document[] = [];
  filteredDocuments: Observable<Document[]>;
  accounts: Account[] = [];
  linkedDocuments: LinkedDocument[] = [];

  constructor(
    public dialogRef: MatDialogRef<TransactionEditComponent>,
    @Inject(MAT_DIALOG_DATA) public transaction: Transaction,
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router
  ) {
    this.transactionFormGroup = this.fb.group({
      transactionNoteControl: new FormControl(transaction.transactionNote),
      netAmountControl: new FormControl(transaction.netAmount),
      grossAmountControl: new FormControl(transaction.grossAmount),
      senderReceiverControl: new FormControl(transaction.senderReceiver),
      taxAmountControl: new FormControl(transaction.taxAmount),
      transactionDateTimeControl: new FormControl(transaction.transactionDateTime),
      isSalesTaxRelevantControl: new FormControl(transaction.isSalesTaxRelevant),
      isIncomeTaxRelevantControl: new FormControl(transaction.isIncomeTaxRelevant),
      accountControl: new FormControl(transaction.accountId)
    });

    this.filteredDocuments = this.documentSearchControl.valueChanges.pipe(
      startWith(''),
      map(value => this._filterDocuments(value))
    );
  }

  onCancelClick(): void {
    this.dialogRef.close();
  }

  private _filterDocuments(value: string | Document | null): Document[] {
    // Filter out documents already linked to this transaction.
    const linkedIds = new Set(this.linkedDocuments.map(d => d.documentId));
    const candidates = this.availableDocuments.filter(d => !linkedIds.has(d.id));

    if (!value || typeof value !== 'string') {
      return candidates;
    }

    const filterValue = value.toLowerCase();
    const filterValueNumber = parseFloat(value);
    const filterValueDate = new Date(value);

    return candidates.filter(document =>
      (
        document.name.toLowerCase().includes(filterValue) ||
        document.total === filterValueNumber ||
        document.invoiceDate === filterValueDate
      )
    );
  }

  onSaveClick(): void {
    const updatedTransaction = {
      ...this.transaction,
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
  }

  ngOnInit(): void {
    this.getLinkedDocuments();
    this.getAvailableDocuments();
    this.getAccounts();
  }

  displayWithDocumentName(document: Document): string {
    return document && document.name ? document.name : '';
  }

  getAvailableDocuments() {
    this.http.get<Document[]>(`/api/documents/getdocuments`).subscribe({
      next: documents => {
        this.availableDocuments = documents;
      },
      error: error => {
        console.error('There was an error fetching documents!', error);
      }
    });
  }

  getLinkedDocuments() {
    this.http.get<TransactionDocumentLink[]>(`/api/transactions/${this.transaction.id}/documents`).subscribe({
      next: links => {
        this.linkedDocuments = links.map(link => ({
          documentId: link.documentId,
          name: link.document?.name ?? `Document ${link.documentId}`,
          sequence: link.sequence
        }));
      },
      error: error => {
        console.error('There was an error fetching linked documents!', error);
      }
    });
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

  onDocumentSelected(document: Document) {
    if (!document || !document.id) {
      return;
    }

    this.http.post(`/api/transactions/${this.transaction.id}/documents`, { documentId: document.id }).subscribe({
      next: () => {
        this.documentSearchControl.setValue('');
        this.getLinkedDocuments();
      },
      error: error => {
        console.error('There was an error linking the document!', error);
      }
    });
  }

  removeDocument(documentId: number) {
    this.http.delete(`/api/transactions/${this.transaction.id}/documents/${documentId}`).subscribe({
      next: () => {
        this.getLinkedDocuments();
      },
      error: error => {
        console.error('There was an error removing the document!', error);
      }
    });
  }
}
