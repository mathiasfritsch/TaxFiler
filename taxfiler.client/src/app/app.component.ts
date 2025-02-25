import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';


@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  public text: any;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.getForecasts();
  }

  getForecasts() {
    this.http.get<string>('/Test').subscribe(
      (result) => {
        this.text = result;
      },
      (error) => {
        console.error(error);
      }
    );
  }

  title = 'taxfiler.client';
}
