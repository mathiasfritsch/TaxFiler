import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AuthInterceptor } from './auth-interceptor';
import { AppComponent } from './app.component';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { TransactionsComponent } from './transactions/transactions.component';
import {RouterModule} from "@angular/router";
import { routes } from './app.routes';
import { DocumentsComponent } from './documents/documents.component';

@NgModule({ declarations: [
        AppComponent,
        TransactionsComponent,
        DocumentsComponent
    ],
    bootstrap: [AppComponent], imports: [BrowserModule, RouterModule.forRoot(routes)], providers: [{ provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }, provideHttpClient(withInterceptorsFromDi())] })
export class AppModule { }
