import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NgFor,NgIf],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'ClientApp';
  documents: any[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.queryDocuments();
  }

  queryDocuments() {
    this.http.get<any[]>('/api/documents?yearMonth=2024-12').subscribe(response => {
      this.documents = response;
    });
  }
}
