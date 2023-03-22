import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { DeviceType } from '../modules/device-type';
import { Device } from '../modules/device';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DeviceTypeService {

  constructor(private http: HttpClient) { }

  getAllDeviceTypes(): Observable<DeviceType[]> {
    let url = environment.baseUrl + 'api/devicetypes';
    return this.http.get<DeviceType[]>(url);
  }

  getDeviceTypeById(id: number): Observable<DeviceType> {
    let url = environment.baseUrl + 'api/devicetypes/' + id;
    return this.http.get<DeviceType>(url);
  }
}
