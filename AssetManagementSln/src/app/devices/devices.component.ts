import { Component, OnInit, ViewChild, ElementRef, TemplateRef, Input, HostListener } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams, HttpEventType } from '@angular/common/http';
import { Router } from '@angular/router';
import { Device } from '../modules/device';
import { environment } from 'src/environments/environment';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { FormGroup, FormControl, Validators, AbstractControl, FormBuilder, FormArray } from '@angular/forms';
import { DeviceType } from '../modules/device-type';
import { Observable } from 'rxjs';
import { transition, style, animate, trigger } from '@angular/animations';
import { BaseService } from '../base.service';
import { DevicesListComponent } from './devices-list/devices-list.component';
import { FileDownloadService } from '../services/file-download.service';
import { DeviceService } from '../services/device.service';

@Component({
  selector: 'app-devices',
  templateUrl: './devices.component.html',
  styleUrls: ['./devices.component.scss'],
})
export class DevicesComponent extends BaseService implements OnInit {

  url: string = environment.baseUrl + 'api/devices';

  displayedColumns: string[] = ['id', 'serviceTag', 'deviceTypeName', 'deviceName', 'poNumber', 'deviceStatusName', 'actions'];

  public allDevices!: MatTableDataSource<Device>;

  public allDeviceTypes?: Observable<DeviceType[]>;
  filterDeviceType: number = 0;

  public devicesArr?: Device[];
//  public deviceToEdit?: Device;

//  public deviceToWithdraw?: Device = <Device>{};

  public deviceToDiscard?: Device;

  public modalTitle?: string;

  public content: any;

  public processing: boolean = false;

  public fileToUpload?: File;

  public fileValid: boolean = false;

  progress?: number;

  public validateErrorMsg?: string;

  public uploadErrorMsg?: string[];

  public fileProcessing: boolean = false;
  public sticky: boolean = false

  @ViewChild('addModal') addModal?: ElementRef;
  @ViewChild('withdrawModal') withdrawModal?: ElementRef;
  @ViewChild('discardModal') discardModal?: ElementRef;
  @ViewChild('uploadModal') uploadModal?: ElementRef;
  @ViewChild('uploadUserDeviceModal') uploadUserDeviceModal?: ElementRef;
  @ViewChild('searchArea') searchArea?: ElementRef;
  @ViewChild(DevicesListComponent) child!: DevicesListComponent;
    
  constructor(
    private http: HttpClient,
    private route: Router,
    private fb: FormBuilder,
    private baseService: BaseService,
    private downloadService: FileDownloadService,
    private deviceService: DeviceService
  ) { 
    super();
  }

  deviceForm = this.fb.group({
    serviceTag: ['', Validators.required, this.deviceService.serviceTagValidatorAsync(0)],
    deviceTypeId: ['', Validators.required, ],
    deviceName: [''],
    acquiredDate: [''],
    deviceModel: [''],
    poNumber: ['']
  })

  uploadForm = this.fb.group({
    excelFile: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadData();
    window.addEventListener('scroll', this.scroll, true); 
  }

  loadData(): void {
    this.loadDevices();
    this.loadDeviceTypeName();
  }

  loadDevices(): void {

    this.http.get<Device[]>(this.url).subscribe({
      next: (result) => {
        this.devicesArr = result;
        this.allDevices = new MatTableDataSource<Device>(result);
        this.allDevices.sort = this.child?.sort;
        this.allDevices.paginator = this.child?.paginator;
      },
      error: (error: HttpErrorResponse) => {
        console.log(error);
        this.generalErrorMessage = error.message;
      }
    });
  }

  loadDeviceTypeName(): void {
    let params = new HttpParams().set("identified", "true");
    let dtUrl = environment.baseUrl + 'api/devicetypes';
    this.allDeviceTypes = this.http.get<DeviceType[]>(dtUrl, { params });
  }

  onSubmit() {
    this.processing = true;
    this.baseService.setProcessingStatusValue(true);
    let device = <Device>{};
    if (device) {
      device.serviceTag = this.deviceForm.controls['serviceTag'].value!;
      device.deviceName = this.deviceForm.controls['deviceName'].value!;
      device.deviceTypeId = this.deviceForm.controls['deviceTypeId'].value!;
      device.acquiredDate = this.deviceForm.controls['acquiredDate'].value!;
      device.deviceModel = this.deviceForm.controls['deviceModel'].value!;
      device.poNumber = this.deviceForm.controls['poNumber'].value!;
      console.log(device);

      this.http.post(this.url, device).subscribe({
        next: (result) => {
          this.processing = false;
          console.log(result);
          this.resetView();
          this.baseService.setProcessingStatusValue(false);
        },
        error: (error: Error) => {
          console.log(error);
          this.generalErrorMessage = error.message;
          this.baseService.setProcessingStatusValue(false);
        }
      });
      
    }
  }

  filterByDeviceType(event: Event) {
    event.preventDefault();
    let filterValue = (event.target as HTMLInputElement).value;
    if (filterValue === "All") {
      this.allDevices = new MatTableDataSource(this.devicesArr);
      this.allDevices.paginator = this.child?.paginator;
    } else {
      let filtered = this.devicesArr?.filter(d => d.deviceTypeName === filterValue.toString());
      this.allDevices = new MatTableDataSource(filtered);
      this.allDevices.paginator = this.child?.paginator;
    }
  }

  resetView(): void {
    if (this.addModal) {
      this.addModal?.nativeElement.click();
    }
    this.deviceForm.reset();
    this.loadData();
  }

  getAddTitle(): void {
    this.modalTitle = "Add New Device";
  }

  getSelectedFile(event: Event): void {
    let files = (event.target as HTMLInputElement).files;
    if (files !== null) {
      this.fileToUpload = files[0];
      this.uploadErrorMsg = undefined;
    }
    if (this.fileToUpload && this.fileToUpload.name.split('.').pop() === 'xlsx') {
      this.fileValid = true;
      this.validateErrorMsg = undefined;
    } else {
      this.fileValid = false;
      this.validateErrorMsg = ('Must be an Excel file (.xlsx).');
    }
  }

  onUpload(): void {
    this.progress = 0;
    this.fileProcessing = true;
    let url = environment.baseUrl + 'api/import/devices';
    let formData = new FormData();
    formData.append('file', this.fileToUpload!, this.fileToUpload!.name);
    this.http.post(url, formData, { reportProgress: true, observe: 'events' }).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress) {
          this.progress = Math.round(100 * event.loaded / event.total!);
        } else if (event.type === HttpEventType.Response) {
          console.log(event.body);
          this.baseService.displayFileUploadSuccessPopup();
          this.uploadModal?.nativeElement.click();
          this.fileProcessing = false;
          this.loadData();
        }
      },
      error: (error: HttpErrorResponse) => {
        if (error.error.length > 1) {
          this.uploadErrorMsg = error.error.split(`\n`);
          this.fileProcessing = false;
        } else {
          this.uploadErrorMsg?.push(error.message);
        }
      }
    });
  }

  onDownload(): void {
    let url = environment.baseUrl + 'api/import/devices';
    this.downloadService.downloadFile(url, "Sample_Devices_Input.xlsx");
  }

  onUploadCancel(): void {
    this.uploadForm.reset();
    this.progress = undefined;
    this.fileValid = false;
    this.uploadErrorMsg = undefined;
  }

  downloadReport(): void {
    let url = environment.baseUrl + 'api/download/devices';
    this.downloadService.downloadFile(url, "report.xlsx");
  }

  downloadAssignation(): void {
    let url = environment.baseUrl + 'api/import/assignation';
    this.downloadService.downloadFile(url, "Sample_assignation.xlsx");
  }

  onUploadAssignation(): void {
    this.progress = 0;
    this.fileProcessing = true;
    let url = environment.baseUrl + 'api/import/assignation';
    let formData = new FormData();
    formData.append('file', this.fileToUpload!, this.fileToUpload!.name);
    this.http.post(url, formData, { reportProgress: true, observe: 'events' }).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress) {
          this.progress = Math.round(100 * event.loaded / event.total!);
        } else if (event.type === HttpEventType.Response) {
          console.log(event.body);
          this.baseService.displayFileUploadSuccessPopup();
          this.uploadUserDeviceModal?.nativeElement.click();
          this.fileProcessing = false;
          this.loadData();
        }
      },
      error: (error: HttpErrorResponse) => {
        console.log(error.error);
        this.uploadErrorMsg = error.error.split(`\n`);
        this.fileProcessing = false;
      }
    });
  }

  scroll = (): void => {
  };

}
