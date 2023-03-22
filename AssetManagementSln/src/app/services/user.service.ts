import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { User } from '../modules/user';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(private http: HttpClient) { }

  getAllActiveUsers(): Observable<User[]> {
    let url = environment.baseUrl + 'api/users';
    return this.http.get<User[]>(url);
  }

  getInfoOfSpecificUser(user: string | null): Observable<User> {
    let url = environment.baseUrl + 'api/users/' + user;
    return this.http.get<User>(url);
  }

  getCurrentDevicesOfUser(user: string | null): Observable<User> {
    let url = environment.baseUrl + 'api/users/devices/' + user;
    return this.http.get<User>(url);
  }
}
