import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { AsyncValidatorFn, AbstractControl } from '@angular/forms';
import { environment } from 'src/environments/environment';
import { Observable, map, BehaviorSubject } from 'rxjs';
import { Device } from '../modules/device';

@Injectable({
  providedIn: 'root'
})
export class DeviceService {
  constructor(protected http: HttpClient) { }

  isDupeServiceTag(serviceTag: string, deviceId: number): Observable<boolean> {
    var params = new HttpParams()
      .set("serviceTag", serviceTag)
      .set("deviceId", deviceId)
    
    var url = environment.baseUrl + 'api/devices/isdupeservicetag';

    return this.http.get<boolean>(url, { params });
  }

  //if we're in edit mode, check if there's another device with the same service tag.
  serviceTagValidatorAsync(deviceId?: number | undefined): AsyncValidatorFn {
    return (control: AbstractControl): Observable<{
      [key: string]: any
    } | null> => {
      return this.isDupeServiceTag(control.value, deviceId!).pipe(map(result => {
        return (result ? { isDupeField: true } : null);
      }));
    }
  }

  getDevicesByStatus(statusId: number): Observable<Device[]> {
    let params = new HttpParams().set("statusId", statusId);
    let url = environment.baseUrl + 'api/devices/status';
    return this.http.get<Device[]>(url, { params });
  }
}
