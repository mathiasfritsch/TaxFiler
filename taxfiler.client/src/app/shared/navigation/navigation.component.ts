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
  @Input() showMonthNavigation: boolean = false;
  @Output() syncClicked = new EventEmitter<void>();
  @Output() monthChanged = new EventEmitter<number>();
  @Output() fileSelected = new EventEmitter<Event>();
  @Output() downloadReportClicked = new EventEmitter<void>();

  onSyncClick(): void {
    this.syncClicked.emit();
  }

  onMonthChange(direction: number): void {
    console.log('Month change requested: ', direction);


    this.monthChanged.emit(direction);
  }

  onFileSelected(event: Event): void {
    this.fileSelected.emit(event);
  }

  onDownloadReportClick(): void {
    this.downloadReportClicked.emit();
  }
}
