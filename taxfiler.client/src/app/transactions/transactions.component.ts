import {Component, OnInit} from '@angular/core';
import {HttpClient} from "@angular/common/http";

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css']
})
export class TransactionsComponent  implements  OnInit{
  public transactions: any[] = [];
  constructor(private http: HttpClient) {}
  ngOnInit() {
    this.getTransactions();
  }
  getTransactions() {
    this.http.get<any[]>('/transactions/gettransactions?yearMonth=2025-01').subscribe(
      (result) => {
        this.transactions = result;
      },
      (error) => {
        console.error(error);
      }
    );
  }
}
