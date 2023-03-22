import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { CurrentUserCart } from '../modules/cart';
import { Observable } from 'rxjs';
import { DeviceTypesOfCart } from '../modules/cart';
import { ProcessInfo } from '../modules/process-info';

@Injectable({
  providedIn: 'root'
})
export class BulkService {

  constructor(private http: HttpClient) { }

  getDevicesOfUserGivenDeviceTypes(userName: string, deviceTypeIds: number[]): Observable<CurrentUserCart> {
    let dtIdStr = JSON.stringify(deviceTypeIds);
    let data = <DeviceTypesOfCart>{};
    data.userName = userName;
    data.deviceTypeIds = deviceTypeIds;
    let url = environment.baseUrl + 'api/bulk/allcurrentdevices';
    return this.http.post<CurrentUserCart>(url, data);
  }

  sendCartToUser(cart: ProcessInfo[]) {
    let url = environment.baseUrl + 'api/bulk';
    return this.http.post(url, cart);
  }

  withdrawAllDevices(userName: string) {
    let url = environment.baseUrl + 'api/bulk/withdraw';
    let params = new HttpParams().set("userName", userName);
    return this.http.get(url, { params });
  }
}
