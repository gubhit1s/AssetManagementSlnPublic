import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { User } from '../modules/user';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PermissionService {

  constructor(private http: HttpClient) { }

  getAllPermittedUsers(): Observable<User[]> {
    let url = environment.baseUrl + 'api/permission/permittedusers';
    return this.http.get<User[]>(url);
  }

  grantAccessToUsers(users: User[]) {
    let url = environment.baseUrl + 'api/permission/permittedusers';
    return this.http.post(url, users);
  }

  deleteUserFromIdentityData(userName: string) {
    let params = new HttpParams().set('userName', userName);
    let url = environment.baseUrl + 'api/permission/permittedusers/revoke';
    return this.http.delete(url, { params });
  }
}
