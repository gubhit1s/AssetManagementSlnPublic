import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, Observable, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { BaseService } from '../base.service';

@Injectable({
    providedIn: 'root'
})

export class AuthInterceptor implements HttpInterceptor {
    
    constructor(private authService: AuthService,
        private router: Router,
        private baseService: BaseService
    ) { }
    
    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        
        //get the auth token.
        let token = this.authService.getToken();

        //if the token is present, clone the request
        //replacing the original headers with the authorization
        if (token) {
            req = req.clone({
                setHeaders: {
                    Authorization: `Bearer ${token}`
                }
            });
        }

        return next.handle(req).pipe(
            catchError((error) => {
                if (error instanceof HttpErrorResponse && error.status === 401) {
                    this.authService.logout();
                    this.router.navigate(['login']);
                    this.baseService.displayExpiredSessionPopup();
                }
                return throwError(() => error);
            })
        );
    }

}