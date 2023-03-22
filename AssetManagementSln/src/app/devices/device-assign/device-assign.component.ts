import { Component, OnInit, ViewChild, ElementRef, Output, EventEmitter } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { FormControl, FormGroup, FormBuilder } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Device } from '../../modules/device';
import { environment } from 'src/environments/environment';
import { User } from '../../modules/user';
import { ProcessInfo } from 'src/app/modules/process-info';
import { BaseService } from '../../base.service';


@Component({
  selector: 'app-device-assign',
  templateUrl: './device-assign.component.html',
  styleUrls: ['./device-assign.component.css']
})
export class DeviceAssignComponent implements OnInit {

  device?: Device;
  id?: number;
  generalErrorMessage?: string;
  userSelected?: User;
  deviceOfUser?: Device;
  userUsingDeviceOfSameType: boolean | undefined = undefined;
  public title: string = "Select a user to assign";

  processing: boolean = false;
  emailSent: boolean = false;

  @ViewChild('confirmModal') confirmModal?: ElementRef;

  constructor(
    private http: HttpClient,
    private router: Router,
    private baseService: BaseService,
    private fb: FormBuilder
  ) { }

  optionsForm = this.fb.group({
    choice: [''],
  });

  ngOnInit(): void {
    this.optionsForm.setValue({
      choice: '3'
    });
  }

  loadDeviceInfo(device: Device): void {
    this.device = device;
  }

  onSelected(user: User): void {
    if (user.displayName) {
      this.userSelected = user;
      console.log(user);
    }
  }

  queryUserDeviceType(): void {
    this.deviceOfUser = undefined;
    if (this.userSelected && this.device) {
      let url = environment.baseUrl + 'api/devices/query';
      var params = new HttpParams()
        .set("userName", this.userSelected.userName)
        .set("deviceTypeId", this.device.deviceTypeId);
      
      this.http.get<Device>(url, { params }).subscribe({
        next: (result) => {
          console.log(result);
          if (result !== null) {
            this.userUsingDeviceOfSameType = true;
            this.deviceOfUser = result;
          } else {
            this.userUsingDeviceOfSameType = false;
          }
        },
        error: (error: HttpErrorResponse) => {
          console.log(error.message);
        }
      });
    }
  }

  resetDialog(): void {
    this.userUsingDeviceOfSameType = undefined;
  }

  //Send a post request to backend to trigger email sending
  onConfirmed(): void {
    this.baseService.setProcessingStatusValue(true);
    let url = environment.baseUrl + 'api/device/confirm';
    this.processing = true;
    if (this.device && this.userSelected) {
      let userDevice: ProcessInfo = { deviceId: this.device.id, userName: this.userSelected.userName, transferTypeId: 0 }
      
      if (this.userUsingDeviceOfSameType) {
        userDevice.transferTypeId = +this.optionsForm.controls['choice'].value!;
      } else {
        userDevice.transferTypeId = 2;
      }
      
      this.http.post<ProcessInfo>(url, userDevice).subscribe({
        next: (result) => {
          console.log(result);
          this.confirmModal?.nativeElement.click();
          this.processing = false;
          this.baseService.displayEmailSentSuccessPopup();
          this.router.navigate(['/devices']);
          this.baseService.setProcessedStatusValue(1);
          this.baseService.setProcessingStatusValue(false);
        },
        error: (error: HttpErrorResponse) => {
          console.log(error);
          this.generalErrorMessage = error.message;
          this.baseService.setProcessingStatusValue(false);
        }
      });
      
    }
  }


}
