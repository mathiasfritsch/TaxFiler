import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AuthInterceptor } from './auth-interceptor';
import { AppComponent } from './app.component';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { TransactionsComponent } from './transactions/transactions.component';
import {RouterModule} from "@angular/router";
import { routes } from './app.routes';

@NgModule({
  declarations: [
    AppComponent,
    TransactionsComponent
  ],
  imports: [
    BrowserModule, HttpClientModule,  RouterModule.forRoot(routes)
  ],
  providers: [{ provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }],
  bootstrap: [AppComponent]
})
export class AppModule { }
