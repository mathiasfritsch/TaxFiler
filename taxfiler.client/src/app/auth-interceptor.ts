import { HttpErrorResponse, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, throwError } from 'rxjs';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<unknown>, next: HttpHandler) {
    const clone = req.clone({
      withCredentials: true,
    });

    return next.handle(clone).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 || error.status === 403 || error.status === 504 || error.status == 0 ) {
          console.info('Unauthorized request, redirecting to login page');
          window.location.href = 'https://127.0.0.1:4200/' +  'MicrosoftIdentity/Account/SignIn';
        }
        return throwError(() => error);
      }),
    );
  }
}
