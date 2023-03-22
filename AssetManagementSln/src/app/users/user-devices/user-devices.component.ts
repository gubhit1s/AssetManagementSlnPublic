import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { environment } from 'src/environments/environment';
import { MatTableDataSource } from '@angular/material/table';
import { Device } from '../../modules/device';
import { User } from '../../modules/user';
import { Observable } from 'rxjs';
import { BaseService } from '../../base.service';
import { DeviceType } from '../../modules/device-type';
import { TooltipPosition } from '@angular/material/tooltip';
import { ProcessInfo } from '../../modules/process-info';
import { DeviceUnidentifiedService } from '../../services/device-unidentified.service';
import { UserService } from 'src/app/services/user.service';
import { BulkService } from 'src/app/services/bulk.service';
import { MatPaginator } from '@angular/material/paginator';
import { DevicesListComponent } from 'src/app/devices/devices-list/devices-list.component';

@Component({
  selector: 'app-user-devices',
  templateUrl: './user-devices.component.html',
  styleUrls: ['./user-devices.component.css']
})
export class UserDevicesComponent extends BaseService implements OnInit {

  devicesOfUser!: MatTableDataSource<Device>;
  devicesUdOfUser!: MatTableDataSource<DeviceType>;
  emptyData: boolean = true;
  
  rightTooltip: TooltipPosition = 'right';

  unidentifiedCols = ['deviceType', 'actions']

  user?: User

  generalErrorMsg?: string;

  allDeviceTypes?: Observable<DeviceType[]>;

  deviceUdSelected?: DeviceType;

  @ViewChild('withdrawUdModal') withdrawUdModal?: ElementRef;
  @ViewChild('withdrawAllModal') withdrawAllModal?: ElementRef;
  @ViewChild(DevicesListComponent) child!: DevicesListComponent;

  constructor(private http: HttpClient, private route: ActivatedRoute,
    private deviceUdService: DeviceUnidentifiedService,
    private baseService: BaseService,
    private userService: UserService,
    private bulkService: BulkService) { 
    super();
  }

  ngOnInit(): void {
    this.loadData();
    this.loadDeviceTypeName();
  }

  loadData(): void {
    let user = this.route.snapshot.paramMap.get('user');
    this.userService.getCurrentDevicesOfUser(user).subscribe({
      next: (result) => {
        this.user = result;
        this.devicesOfUser = new MatTableDataSource<Device>(result.devices);
        this.devicesOfUser.paginator = this.child.paginator;
        this.devicesUdOfUser = new MatTableDataSource<DeviceType>(result.devicesUnidentified);
        if (result.devices!.length + result.devicesUnidentified!.length > 0) {
          this.emptyData = false;
        }
      },
      error: (error: HttpErrorResponse) => {
        this.generalErrorMessage = error.message + error.error;
        console.log(error);
      }
    });
  }

  loadDeviceTypeName(): void {
    let dtUrl = environment.baseUrl + 'api/devicetypes';
    this.allDeviceTypes = this.http.get<DeviceType[]>(dtUrl);
  }

  getDeviceUdToWithdraw(deviceType: DeviceType): void {
    this.deviceUdSelected = deviceType;
  }

  onWithdrawUd(): void {
    this.baseService.setProcessingStatusValue(true);
    let data = <ProcessInfo>{};
    data.deviceTypeId = this.deviceUdSelected?.id;
    data.userName = this.user!.userName;
    data.transferTypeId = 4;
    this.deviceUdService.sendConfirmData(data).subscribe({
      next: () => {
        this.withdrawUdModal?.nativeElement.click();
        this.baseService.setProcessingStatusValue(false);
        this.baseService.displayEmailSentSuccessPopup();
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.error;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }

  onWithdrawAll(): void {
    this.baseService.setProcessingStatusValue(true);
    let userName = this.user!.userName;
    this.bulkService.withdrawAllDevices(userName).subscribe({
      next: () => {
        this.withdrawAllModal?.nativeElement.click();
        this.baseService.setProcessingStatusValue(false);
        this.baseService.displayEmailSentSuccessPopup();
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }


}
