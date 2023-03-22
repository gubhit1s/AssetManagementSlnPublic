import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DeviceOverview } from '../modules/device-overview';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class HomeService {

  constructor(private http: HttpClient) { }

  getDeviceOverviews(): Observable<DeviceOverview[]> {
    let url = environment.baseUrl + 'api/dashboard/deviceoverviews';
    return this.http.get<DeviceOverview[]>(url);
  }
}
