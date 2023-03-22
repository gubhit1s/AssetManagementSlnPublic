import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, tap } from 'rxjs';

import { UserForAuthentication } from './user-for-authentication';
import { LoginResult } from './login-result';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private tokenKey: string = "token";
  private _authStatus = new Subject<boolean>();
  public authStatus = this._authStatus.asObservable();

  constructor(private http: HttpClient) { }

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  init(): void {
    if (this.isAuthenticated()) {
      this.setAuthStatus(true)
    } else {
      this.setAuthStatus(false)
    };
  }

  private setAuthStatus(isAuthenticated: boolean): void {
    this._authStatus.next(isAuthenticated);
  }

  login(item: UserForAuthentication): Observable<LoginResult> {
    let url = environment.baseUrl + "api/account/login";
    return this.http.post<LoginResult>(url, item).pipe(tap(loginResult => {
      if (loginResult.success && loginResult.token) {
        localStorage.setItem(this.tokenKey, loginResult.token);
        this.setAuthStatus(true);
      }
    }));
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    this.setAuthStatus(false);
  }

}
