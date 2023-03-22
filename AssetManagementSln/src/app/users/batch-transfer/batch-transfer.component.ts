import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { DeviceTypeService } from 'src/app/services/device-type.service';
import { DeviceType } from 'src/app/modules/device-type';
import { Device } from 'src/app/modules/device';
import { DeviceService } from 'src/app/services/device.service';
import { MatTableDataSource } from '@angular/material/table';
import { BaseService } from 'src/app/base.service';
import { MatChipEvent, MatChipListChange, MatChipSelectionChange } from '@angular/material/chips';
import { MatSort } from '@angular/material/sort';
import { MatButtonToggleChange } from '@angular/material/button-toggle';
import { UserService } from 'src/app/services/user.service';
import { User } from 'src/app/modules/user';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DeviceUnidentified } from 'src/app/modules/device-unidentified';
import { DeviceUnidentifiedService } from 'src/app/services/device-unidentified.service';
import { CurrentUserCart, CartDetail } from 'src/app/modules/cart';
import { BulkService } from 'src/app/services/bulk.service';
import { environment } from 'src/environments/environment';
import { Form, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ProcessInfo } from 'src/app/modules/process-info';
import { MatPaginator } from '@angular/material/paginator';

@Component({
  selector: 'app-batch-transfer',
  templateUrl: './batch-transfer.component.html',
  styleUrls: ['./batch-transfer.component.scss']
})
export class BatchTransferComponent extends BaseService implements OnInit {

  allDeviceTypes?: DeviceType[];
  @ViewChild(MatSort) sort!: MatSort;
  devicesArr?: Device[];
  devicesUdArr?: DeviceUnidentified[];
  inStockDevices!: MatTableDataSource<Device>;
  inStockUdDevices!: MatTableDataSource<DeviceUnidentified>;
  user?: User;
  deviceTypeIdsAdded: number[] = [];
  cartDetails: CartDetail[] = [];
  identified: boolean = true;
  outOfStock: boolean = false;
  deviceSelected?: string;
  deviceTypeIdSelected: number = 1;  //Default laptop
  cartCount: number = 0;
  allCurrentDevices?: CurrentUserCart;
  @ViewChild("confirmModal") confirmModal?: ElementRef;
  @ViewChild("cartReviewModal") cartReviewModal?: ElementRef;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  displayedColumns: string[] = ['actions', 'serviceTag', 'deviceName', 'acquiredDate'];
  displayedUdColumns: string[] = ['actions', 'stockAmount'];

  optionsForm?: FormGroup;

  constructor(private deviceTypeService: DeviceTypeService,
    private deviceService: DeviceService,
    private userService: UserService,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar,
    private deviceUdService: DeviceUnidentifiedService,
    private bulkService: BulkService,
    private fb: FormBuilder,
    private baseService: BaseService,
    private router: Router
  ) {
    super();
   }

  ngOnInit(): void {
    this.loadDeviceTypes();
    this.loadInStockDevices();
    this.loadInStockUnidentifiedDevices();
    this.loadUserInfo();
  }

  loadDeviceTypes(): void {
    this.deviceTypeService.getAllDeviceTypes().subscribe({
      next: (result) => {
        this.allDeviceTypes = result;
        this.deviceTypeIdSelected = result[0].id;
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
      }
    });
  }

  loadInStockDevices(): void {
    this.deviceService.getDevicesByStatus(1).subscribe({
      next: (result) => {
        this.devicesArr = result;
        //Filter first value
        let filter = result.filter(d => Number(d.deviceTypeId) === this.deviceTypeIdSelected);
        this.inStockDevices = new MatTableDataSource(filter);
        this.inStockDevices.sort = this.sort;
        this.inStockDevices.paginator = this.paginator;
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
      }
    });
  }

  loadInStockUnidentifiedDevices(): void {
    this.deviceUdService.getDevicesByStatusId(1).subscribe({
      next: (result) => {
        this.devicesUdArr = result;
        this.inStockUdDevices = new MatTableDataSource(result);
        this.inStockUdDevices.sort = this.sort;
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
      }
    });
  }


  loadUserInfo(): void {
    let user = this.route.snapshot.paramMap.get('user');
    this.userService.getInfoOfSpecificUser(user).subscribe({
      next: (result) => {
        this.user = result;
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
      }
    });
  }

  select(event: MatChipSelectionChange) {
    event.source.focus();
  }

  filterDevicesByType(event: MatButtonToggleChange) {
    let dtId = event.value;
    this.deviceTypeIdSelected = dtId;
    let deviceType = this.allDeviceTypes?.find(d => d.id === dtId);
    if (deviceType && deviceType.isIdentified) {
      this.identified = true;
      //Filter array by device type.
      let filter = this.devicesArr?.filter(d => d.deviceTypeId === dtId);
      this.outOfStock = (filter && filter!.length <= 0) ? true : false;
      this.inStockDevices = new MatTableDataSource(filter);
      this.inStockDevices.sort = this.sort;
      this.inStockDevices.paginator = this.paginator;
      //Append service tag to remove button.
      let device = this.cartDetails.find(d => d.deviceTypeId === dtId);
      this.deviceSelected = device ? "(" + device.serviceTag + ")" : undefined;

    } else {
      this.identified = false;
      //Filter array by device type.
      let filter = this.devicesUdArr?.filter(du => du.deviceTypeId === event.value);
      this.outOfStock = (filter![0].amount === 0) ? true : false;
      this.inStockUdDevices = new MatTableDataSource(filter);
      //Remove service tag from remove button.
      let deviceUd = this.cartDetails.find(d => d.deviceTypeId === dtId);
      this.deviceSelected = deviceUd ? "(" + deviceUd.deviceTypeName + ")" : undefined;
    }
  }

  addToCart(device: Device | DeviceUnidentified): void {
    if (this.identified) {
      let deviceIdentified = device as Device;
      this.deviceTypeIdsAdded.push(Number(deviceIdentified.deviceTypeId));
      this.deviceSelected = "(" + deviceIdentified.serviceTag + ")";

      this.cartCount++;
      let item = <CartDetail>{};
      item.deviceId = deviceIdentified.id;
      item.deviceTypeId = +deviceIdentified.deviceTypeId;
      item.deviceTypeName = deviceIdentified.deviceTypeName;
      item.serviceTag = deviceIdentified.serviceTag;
      item.deviceName = deviceIdentified.deviceName;

      this.cartDetails.push(item);

    } else {
      let deviceUnidentified = device as DeviceUnidentified;
      this.deviceTypeIdsAdded.push(Number(deviceUnidentified.deviceTypeId));

      let item = <CartDetail>{};
      item.deviceTypeId = deviceUnidentified.deviceTypeId;
      item.deviceTypeName = deviceUnidentified.deviceTypeName;

      this.cartDetails.push(item);
      this.deviceSelected = "(" + deviceUnidentified.deviceTypeName + ")";
    }
    
    this.snackBar.open("Device added to Cart", "Dismiss", { duration: 1000 });
  }

  removeFromCart(): void {
    let id = this.deviceTypeIdSelected;
    //remove item from cart using the splice() method.
    if (this.identified) {
      let deviceToRemoveInd = this.cartDetails.findIndex(d => Number(d.deviceTypeId) === id);
      this.cartDetails.splice(deviceToRemoveInd, 1);
    } else {
      let deviceUdToRemoveInd = this.cartDetails.findIndex(d => d.deviceTypeId === id);
      this.cartDetails.splice(deviceUdToRemoveInd, 1);
    }

    this.deviceTypeIdsAdded.splice(this.deviceTypeIdsAdded.indexOf(id), 1);
    this.deviceSelected = undefined;
    this.snackBar.open("Device removed from cart", "Dismiss", { duration: 1000 });
    this.cartCount--;
  }

  enumerateCart(): void {
    let id = 1;
    this.cartDetails.forEach((c) => {
      c.itemId = id;
      id++;
    });
  }

  getCurrentDevices(): void {
    this.bulkService.getDevicesOfUserGivenDeviceTypes(this.user!.userName, this.deviceTypeIdsAdded).subscribe({
      next: (result) => {
        console.log(result);
        this.allCurrentDevices = result;
        this.allCurrentDevices.count = result.currentDevicesIdentified.length + result.currentDevicesUnidentified.length;
        
        let controls: { [key: number]: FormControl } = {};
        result.currentDevicesIdentified.forEach(d => controls[Number(d.deviceTypeId)] = new FormControl('', Validators.required));
        result.currentDevicesUnidentified.forEach(d => controls[d.deviceTypeId] = new FormControl('', Validators.required));
        this.optionsForm = this.fb.group(controls);
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
      }
    });
  }

  prepareCart(): ProcessInfo[] {
    let transfers: ProcessInfo[] = [];
    this.cartDetails.forEach(c => {
      let transfer = <ProcessInfo>{};
      if (c.deviceId) transfer.deviceId = c.deviceId;
      transfer.deviceTypeId = c.deviceTypeId;
      transfer.userName = this.user!.userName;

      //if control of this device type exists, then take its control value as transfer id, otherwise it is 2 (assignation).
      if (this.optionsForm && this.optionsForm.controls[c.deviceTypeId]) {
        transfer.transferTypeId = this.optionsForm.controls[c.deviceTypeId].value;
      } else {
        transfer.transferTypeId = 2;
      };
      transfers.push(transfer);
    });
    console.log(transfers);
    return transfers;
  }

  onBulkConfirm(): void {
    this.baseService.setProcessingStatusValue(true);
    let data = this.prepareCart();
    this.bulkService.sendCartToUser(data).subscribe({
      next: () => {
        this.cartReviewModal?.nativeElement.click();
        this.confirmModal?.nativeElement.click();
        this.router.navigate(['users', this.user?.userName]);
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
