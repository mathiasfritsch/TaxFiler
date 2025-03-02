import {Component, OnInit} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-transactions',
  templateUrl: './transactions.component.html',
  styleUrls: ['./transactions.component.css']
})
export class TransactionsComponent  implements  OnInit{
  public transactions: any[] = [];
  constructor(private http: HttpClient,private route: ActivatedRoute) {}
  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.getTransactions(params.get('yearMonth'));
    });
  }
  getTransactions(yearMonth: any) {
    console.log(yearMonth);
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
