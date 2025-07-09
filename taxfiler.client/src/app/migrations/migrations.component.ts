import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

@Component({
  selector: 'app-migrations',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    RouterModule,
    MatSnackBarModule,
  ],
  templateUrl: './migrations.component.html',
  styleUrls: ['./migrations.component.css']
})
export class MigrationsComponent {
  isRunning = false;
  lastResult = '';

  constructor(
    private http: HttpClient,
    private snackBar: MatSnackBar
  ) { }

  runMigrations(): void {
    this.isRunning = true;
    this.lastResult = '';
    
    this.http.get('/api/db/RunMigrations', { responseType: 'text' }).subscribe({
      next: (result) => {
        this.isRunning = false;
        this.lastResult = result;
        if (result === 'ok') {
          this.snackBar.open('Migrations completed successfully!', 'Close', {
            duration: 5000,
            panelClass: 'success-snack'
          });
        } else {
          this.snackBar.open('Migration completed with message: ' + result, 'Close', {
            duration: 10000,
            panelClass: 'warning-snack'
          });
        }
      },
      error: (error) => {
        this.isRunning = false;
        this.lastResult = 'Error: ' + error.message;
        this.snackBar.open('Error running migrations: ' + error.message, 'Close', {
          duration: 10000,
          panelClass: 'error-snack'
        });
      }
    });
  }
}