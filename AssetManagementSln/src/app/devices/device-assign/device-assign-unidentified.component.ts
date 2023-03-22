import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseService } from 'src/app/base.service';
import { DeviceType } from 'src/app/modules/device-type';
import { DeviceTypeService } from 'src/app/services/device-type.service';
import { User } from 'src/app/modules/user';
import { Form, FormBuilder } from '@angular/forms';
import { DeviceUnidentifiedService } from 'src/app/services/device-unidentified.service';
import { environment } from 'src/environments/environment';
import { ProcessInfo } from 'src/app/modules/process-info';

@Component({
  selector: 'app-device-assign-unidentified',
  templateUrl: './device-assign-unidentified.component.html',
  styleUrls: ['./device-assign-unidentified.component.css']
})
export class DeviceAssignUnidentifiedComponent extends BaseService implements OnInit {

  title: string = "";
  deviceType?: DeviceType;
  userSelected?: User;
  userUsingDeviceOfSameType?: boolean | undefined = undefined;

  optionsForm = this.fb.group({
    choice: [''],
  });

  @ViewChild('confirmUdModal') confirmUdModal?: ElementRef;


  constructor(private baseService: BaseService, private route: ActivatedRoute,
    private deviceTypeService: DeviceTypeService,
    private deviceUdService: DeviceUnidentifiedService,
    private fb: FormBuilder,
    private router: Router
  ) { 
    super();
  }

  ngOnInit(): void {
    let idParam = this.route.snapshot.paramMap.get('id');
    let id = idParam === null ? 0 : +idParam;
    this.deviceTypeService.getDeviceTypeById(id).subscribe({
      next: (result) => {
        this.deviceType = result;
        this.title = result.name + " | Select a user to assign";
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
      }
    });
    this.optionsForm.setValue({
      choice: '3'
    });
  }

  onSelected(user: User): void {
    if (user.displayName) {
      this.userSelected = user;
      console.log(user);
    }
  }

  queryUserDeviceTypeUd(): void {
    if (this.userSelected && this.deviceType) {
      
      this.deviceUdService.getUserSameDeviceType(this.deviceType.id, this.userSelected.userName).subscribe({
        next: (result) => {
          this.userUsingDeviceOfSameType = result;
        },
        error: (error: HttpErrorResponse) => {
          console.log(error.error);
          this.generalErrorMessage = error.message;
        }
      });
    }
  }

  resetDialog(): void {
    this.userUsingDeviceOfSameType = undefined;
  }

  onConfirmed(): void {
    this.baseService.setProcessingStatusValue(true);
    let confirmDetailsUd = <ProcessInfo>{};
    confirmDetailsUd.deviceTypeId = this.deviceType!.id;
    confirmDetailsUd.userName = this.userSelected!.userName;
    if (this.userUsingDeviceOfSameType) {
      confirmDetailsUd.transferTypeId = +this.optionsForm.controls['choice'].value!;
    } else {
      confirmDetailsUd.transferTypeId = 2;
    }

    this.deviceUdService.sendConfirmData(confirmDetailsUd).subscribe({
      next: () => {
        this.confirmUdModal?.nativeElement.click();
        this.baseService.displayEmailSentSuccessPopup();
        this.router.navigate(['devices/unidentified']);
        this.baseService.setProcessingStatusValue(false);
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }


}
