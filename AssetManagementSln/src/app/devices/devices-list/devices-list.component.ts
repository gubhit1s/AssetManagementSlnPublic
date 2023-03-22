import { Component, OnInit, ViewChild, ElementRef, TemplateRef, Input, EventEmitter, Output, AfterViewInit } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Router } from '@angular/router';
import { Device } from '../../modules/device';
import { environment } from 'src/environments/environment';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { FormGroup, FormControl, Validators, AbstractControl, FormBuilder, FormArray, AsyncValidatorFn } from '@angular/forms';
import { DeviceType } from '../../modules/device-type';
import { Observable, map } from 'rxjs';
import { transition, style, animate, trigger } from '@angular/animations';
import { BaseService } from '../../base.service';
import { ProcessInfo } from 'src/app/modules/process-info';
import { User } from '../../modules/user';
import { DeviceService } from '../../services/device.service';
import { MatPaginator } from '@angular/material/paginator';


@Component({
  selector: 'app-devices-list',
  templateUrl: './devices-list.component.html',
  styleUrls: ['./devices-list.component.scss']
})
export class DevicesListComponent extends BaseService implements OnInit {

  url: string = environment.baseUrl + 'api/devices';
  displayedColumns: string[] = ['deviceTypeName', 'serviceTag', 'deviceName', 'acquiredDate', 'deviceStatusName', 'actions'];

  @Input() deviceTypes?: Observable<DeviceType[]>;

  @Input() errorMessage?: string;

  public deviceToEdit?: Device;

  public deviceToWithdraw?: Device = <Device>{};

  public deviceToProcess?: Device;

  public modalTitle?: string;

  public content: any;

  public currentUser?: User;

  @Input() devices!: MatTableDataSource<Device>;
  @Output() dataChanged = new EventEmitter();

  @ViewChild('editModal') editModal?: ElementRef;
  @ViewChild('withdrawModal') withdrawModal?: ElementRef;
  @ViewChild('discardModal') discardModal?: ElementRef;
  @ViewChild('maintainModal') maintainModal?: ElementRef;
  @ViewChild('maintainDoneModal') maintainDoneModal?: ElementRef;
  @ViewChild('restockModal') restockModal?: ElementRef;
  @ViewChild('decommissionModal') decommissionModal?: ElementRef;
  @ViewChild(MatSort) sort!: MatSort;

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  deviceForm = this.fb.group({
    serviceTag: ['', Validators.required],
    deviceTypeId: ['', Validators.required],
    deviceName: [''],
    acquiredDate: [''],
    deviceModel: [''],
    poNumber: ['']
  })

  constructor(
    private http: HttpClient,
    private route: Router,
    private fb: FormBuilder,
    private baseService: BaseService,
    private deviceService: DeviceService
  ) { 
    super();
  }

  ngOnInit(): void {
  }

  onSubmit() {
    this.baseService.setProcessingStatusValue(true);
    let device = this.deviceToEdit ? this.deviceToEdit : <Device>{};
    if (device) {
      device.serviceTag = this.deviceForm.controls['serviceTag'].value!;
      device.deviceName = this.deviceForm.controls['deviceName'].value!;
      device.deviceTypeId = this.deviceForm.controls['deviceTypeId'].value!
      device.acquiredDate = this.deviceForm.controls['acquiredDate'].value!;
      device.deviceModel = this.deviceForm.controls['deviceModel'].value!;
      device.poNumber = this.deviceForm.controls['poNumber'].value!;
      console.log(device);

      if (device.id) {
        let putUrl = this.url + "/" + device.id;
        this.http.put(putUrl, device).subscribe({
          next: (result) => {
            this.resetView();
            this.baseService.setProcessingStatusValue(false);
          },
          error: (error: Error) => {
            console.log(error);
            this.errorMessage = error.message;
            this.baseService.setProcessingStatusValue(false);
          }
        });
      } else {
        this.errorMessage = "Cannot find a device to edit";
      }
      
    }
  }


  resetView(): void {
    if (this.editModal) {
      this.editModal?.nativeElement.click();
    }
    this.deviceToEdit = undefined;
    this.deviceForm.reset();
    this.dataChanged.emit();
  }

  getDeviceDataForEditing(device: Device): void {
    this.deviceForm.get('serviceTag')!.clearAsyncValidators();
    this.modalTitle = "Edit Device";
    this.deviceToEdit = device;
    this.deviceForm.patchValue(this.deviceToEdit);
    this.deviceForm.get('serviceTag')!.addAsyncValidators(this.deviceService.serviceTagValidatorAsync(this.deviceToEdit?.id));
    console.log(this.deviceToEdit);
  }

  getUserOfDeviceToWithdraw(device: Device): void {
    let url = this.url + "/current?deviceId=" + device.id;
    this.deviceToWithdraw = device;
    this.http.get<User>(url).subscribe({
      next: (result) => {
        this.currentUser = result;
      },
      error: (error: HttpErrorResponse) => {
        console.log(error.error);
        this.errorMessage = error.message;
      }
    });
  }

  getDeviceToProcess(device: Device): void {
    this.deviceToProcess = device;
  }

  onWithdraw(): void {
    this.baseService.setProcessingStatusValue(true);
    let data: ProcessInfo = {
      userName: this.currentUser!.userName,
      deviceId: this.deviceToWithdraw!.id,
      transferTypeId: 4
    };
    let url = environment.baseUrl + 'api/device/confirm';
    this.http.post(url, data).subscribe({
      next: (result) => {
        this.clearModal(this.withdrawModal);
        this.baseService.displayEmailSentSuccessPopup();
      },
      error: (error: HttpErrorResponse) => {
        console.log(error);
        this.errorMessage = error.message + error.error;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }

  onMaintain(): void {
    this.baseService.setProcessingStatusValue(true);
    let params = new HttpParams().set("deviceId", this.deviceToProcess!.id);
    let url = environment.baseUrl + 'api/devices/repairing';
    this.http.get(url, { params }).subscribe({
      next: () => {
        this.clearModal(this.maintainModal);
        this.baseService.displayRepairingSuccessPopup();
      },
      error: (error: HttpErrorResponse) => {
        console.log(error);
        this.errorMessage = error.message;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }

  onRecover(repairSuccess: boolean): void {
    this.baseService.setProcessingStatusValue(true);
    let params = new HttpParams()
      .set("deviceId", this.deviceToProcess!.id)
      .set("repairSuccess", repairSuccess);
    
    let url = environment.baseUrl + 'api/devices/repaired';
    this.http.get(url, { params }).subscribe({
      next: () => {
        this.clearModal(this.maintainDoneModal);
        this.baseService.displayMoveBackToStockSuccessPopup();
      },
      error: (error: HttpErrorResponse) => {
        console.log(error);
        this.errorMessage = error.message;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }
  onDecommissioned(): void {
    this.baseService.setProcessingStatusValue(true);
    let params = new HttpParams().set("deviceId", this.deviceToProcess!.id);
    let url = environment.baseUrl + 'api/devices/decommissioned';
    this.http.get(url, { params }).subscribe({
      next: (result) => {
        this.clearModal(this.decommissionModal);
        this.baseService.displayNotification(7);
      },
      error: (error: HttpErrorResponse) => {
        console.log(error);
        this.errorMessage = error.message;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }

  clearModal(modal?: ElementRef): void {
    modal?.nativeElement.click();
    this.dataChanged.emit();
    this.generalErrorMessage = undefined;
    this.baseService.setProcessingStatusValue(false);
  }

  removeDupeAsyncValidators(): void {
    this.deviceForm.get('serviceTag')!.clearAsyncValidators();
  }
  

}
