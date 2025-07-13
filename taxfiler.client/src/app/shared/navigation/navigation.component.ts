import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { RouterModule } from '@angular/router';

export interface NavigationAction {
  label: string;
  action: () => void;
}

@Component({
  selector: 'app-navigation',
  standalone: true,
  imports: [CommonModule, MatButtonModule, RouterModule],
  templateUrl: './navigation.component.html',
  styleUrls: ['./navigation.component.css']
})
export class NavigationComponent {
  @Input() yearMonth?: string;
  @Input() showDocuments: boolean = false;
  @Input() showTransactions: boolean = false;
  @Input() showAccounts: boolean = false;
  @Input() showMigrations: boolean = true;
  @Input() showSync: boolean = false;
  @Input() showMonthNavigation: boolean = false;
  @Input() showUpload: boolean = false;
  @Input() showDownloadReport: boolean = false;
  @Input() customActions: NavigationAction[] = [];

  @Output() syncClicked = new EventEmitter<void>();
  @Output() monthChanged = new EventEmitter<number>();
  @Output() fileSelected = new EventEmitter<Event>();
  @Output() downloadReportClicked = new EventEmitter<void>();

  onSyncClick(): void {
    this.syncClicked.emit();
  }

  onMonthChange(direction: number): void {
    this.monthChanged.emit(direction);
  }

  onFileSelected(event: Event): void {
    this.fileSelected.emit(event);
  }

  onDownloadReportClick(): void {
    this.downloadReportClicked.emit();
  }

  onCustomAction(action: NavigationAction): void {
    action.action();
  }
}
