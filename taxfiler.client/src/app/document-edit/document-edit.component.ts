import {Component, Inject} from "@angular/core";
import {MatFormField} from "@angular/material/form-field";
import {FormsModule} from "@angular/forms";
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

export interface DialogData {
  animal: string;
  name: string;
}

@Component({
  selector: 'dialog-overview-example-dialog',
  templateUrl: '../document-edit/document-edit.component.html',
  imports: [
    MatFormField,
    FormsModule,
    MatDialogClose,
    MatDialogActions,
    MatButton,
    MatInput,
    MatDialogContent,
    MatDialogTitle
  ]
})
export class DialogOverviewExampleDialog {

  constructor(
    public dialogRef: MatDialogRef<DialogOverviewExampleDialog>,
    @Inject(MAT_DIALOG_DATA) public data: DialogData) {}

  onCancelClick(): void {
    this.dialogRef.close();
  }

}
