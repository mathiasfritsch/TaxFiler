import { Component, Inject } from '@angular/core';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle
} from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { AutoAssignResult } from '../model/auto-assign-result';

@Component({
  selector: 'app-auto-assign-result-dialog',
  templateUrl: './auto-assign-result-dialog.component.html',
  styleUrls: ['./auto-assign-result-dialog.component.css'],
  standalone: true,
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatButton
  ]
})
export class AutoAssignResultDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<AutoAssignResultDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public result: AutoAssignResult
  ) {}

  onClose(): void {
    this.dialogRef.close();
  }
}

