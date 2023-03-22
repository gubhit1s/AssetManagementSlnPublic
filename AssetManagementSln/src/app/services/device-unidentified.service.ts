import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DeviceUnidentified } from '../modules/device-unidentified';
import { environment } from 'src/environments/environment';
import { Device } from '../modules/device';
import { DeviceType } from '../modules/device-type';
import { ProcessInfo } from '../modules/process-info';

@Injectable({
  providedIn: 'root'
})

export class DeviceUnidentifiedService {

  constructor(private http: HttpClient) { }

  getAllRecords(): Observable<DeviceUnidentified[]> {
    let url = environment.baseUrl + 'api/devices/unidentified';
    return this.http.get<DeviceUnidentified[]>(url);
  }

  sendInputData(data: DeviceUnidentified) {
    let url = environment.baseUrl + 'api/devices/unidentified';
    return this.http.post(url, data);
  }

  getUserSameDeviceType(deviceTypeId: number, userName: string): Observable<boolean> {
    let url = environment.baseUrl + 'api/devices/unidentified/query';
    let params = new HttpParams()
      .set('deviceTypeId', deviceTypeId)
      .set('userName', userName);
    return this.http.get<boolean>(url, { params });
  }

  sendConfirmData(data: ProcessInfo) {
    let url = environment.baseUrl + 'api/device/unidentified/confirm';
    return this.http.post(url, data);
  }

  uploadDeviceUserUd(formData: FormData) {
    let url = environment.baseUrl + 'api/import/assignation/unidentified';
    return this.http.post(url, formData, { reportProgress: true, observe: 'events' });
  }

  getDevicesByStatusId(statusId: number): Observable<DeviceUnidentified[]> {
    let url = environment.baseUrl + 'api/devices/unidentified';
    let params = new HttpParams()
      .set('statusId', statusId)
    return this.http.get<DeviceUnidentified[]>(url, { params });
  }

}
