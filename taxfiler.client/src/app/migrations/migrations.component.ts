import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { MatButton } from '@angular/material/button';
import { MatDialogTitle } from '@angular/material/dialog';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-migrations',
  imports: [
    RouterLink,
    MatButton,
    MatDialogTitle,
    NgIf
  ],
  templateUrl: './migrations.component.html',
  styleUrl: './migrations.component.css'
})
export class MigrationsComponent {
  public isRunning = false;
  public result: string | null = null;
  public hasError = false;

  constructor(private http: HttpClient) {}

  getStartOfMonth(): string {
    const date = new Date();
    const firstDay = new Date(date.getFullYear(), date.getMonth(), 1);
    const year = firstDay.getFullYear();
    const month = (firstDay.getMonth() + 1).toString().padStart(2, '0');
    return `${year}-${month}`;
  }

  runMigrations() {
    this.isRunning = true;
    this.result = null;
    this.hasError = false;

    this.http.get('/api/DB/RunMigrations', { responseType: 'text' }).subscribe({
      next: (response) => {
        this.result = response;
        this.hasError = false;
        this.isRunning = false;
      },
      error: (error) => {
        this.result = error.error || 'An error occurred while running migrations';
        this.hasError = true;
        this.isRunning = false;
      }
    });
  }
}
