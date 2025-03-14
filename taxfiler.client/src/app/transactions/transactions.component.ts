import {Component, OnInit} from '@angular/core';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import {ActivatedRoute, Router} from '@angular/router';

@Component({
    selector: 'app-transactions',
    templateUrl: './transactions.component.html',
    styleUrls: ['./transactions.component.css'],
    standalone: false
})
export class TransactionsComponent  implements  OnInit{
  public transactions: any[] = [];
  public yearMonth: any;
  constructor(private http: HttpClient, private route: ActivatedRoute, private router: Router) {}
  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.yearMonth = params.get('yearMonth');
      this.getTransactions(this.yearMonth);
    });
  }
  getTransactions(yearMonth: any) {
    console.log(yearMonth);
    this.http.get<any[]>(`/api/transactions/gettransactions?yearMonth=${yearMonth}`).subscribe(
      {
        next: transactions => {
          this.transactions = transactions;
        },
        error: error => {
          console.error('There was an error!', error);
        }
      }
    );
  }
  switchMonth(offset: number) {
    const [year, month] = this.yearMonth.split('-').map(Number);
    const date = new Date(year, month - 1 + offset, 1);
    const newYearMonth = `${date.getFullYear()}-${(date.getMonth() + 1).toString().padStart(2, '0')}`;
    this.router.navigate([`/transactions/${newYearMonth}`]).then();
  }
  onFileSelected(event: any): void {
    debugger
    const file: File = event.target.files[0];
    if (file) {
      const formData = new FormData();
      formData.append('file', file);

      const headers = new HttpHeaders();
      headers.append('Content-Type', 'multipart/form-data');

      this.http.post('/api/transactions/upload?', formData, { headers })
        .subscribe(
          {
            next: () => {
              this.getTransactions(this.yearMonth);
            },
            error: error => {
              console.error('There was an error!', error);
            }
          }
        );
    }
  }
}
