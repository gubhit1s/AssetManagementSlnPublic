import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpEventType } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { DeviceUnidentifiedService } from '../services/device-unidentified.service';
import { DeviceUnidentified } from '../modules/device-unidentified';
import { Device } from '../modules/device';
import { BaseService } from '../base.service';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { FormBuilder, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { TooltipPosition } from '@angular/material/tooltip';
import { FileDownloadService } from '../services/file-download.service';
import { filter } from 'rxjs';
import { environment } from 'src/environments/environment';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-devices-unidentified',
  templateUrl: './devices-unidentified.component.html',
  styleUrls: ['./devices-unidentified.component.scss'],
  animations: [
    trigger('detailExpand', [
      state('collapsed', style({ height: '0px', minHeight: '0' })),
      state('expanded', style({ height: '*' })),
      transition('expanded <=> collapsed', animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')),
    ]),
  ],
})
export class DevicesUnidentifiedComponent extends BaseService implements OnInit {

  public devicesUnidentified!: MatTableDataSource<DeviceUnidentified>;

  public devicesUnidentifiedTotal!: MatTableDataSource<DeviceUnidentified>;

  public displayedColumns: string[] = ['deviceTypeName', 'deviceStatusName', 'amount', 'expand'];
  public detailColumns: string[] = ['deviceTypeName', 'deviceStatusName', 'amount', 'actions'];
  
  title?: string;
  modalInstruction?: string;

  expandedElement?: DeviceUnidentified | null;

  selectedElement?: DeviceUnidentified;
  
  public fileToUpload?: File;

  public fileValid: boolean = false;

  progress?: number;

  public validateErrorMsg?: string;

  public uploadErrorMsg?: string[];

  public fileProcessing: boolean = false;

  tooltipAfter: TooltipPosition = 'after';
  @ViewChild('actionModal') actionModal?: ElementRef;
  @ViewChild('uploadUserDeviceModalUd') uploadUserDeviceModalUd?: ElementRef;

  constructor(private deviceUnidentifiedService: DeviceUnidentifiedService, private fb: FormBuilder,
    private baseService: BaseService, private downloadService: FileDownloadService
  ) { 
    super();
  }

  deviceUnidentifiedForm = this.fb.group({
    amount: ['', [Validators.required, Validators.pattern(/^\d+$/), this.positiveNumberValidator(), this.numberNotExceedCurrentValidator()]]
  })

  editForm = this.fb.group({
    amount: ['', [Validators.required, Validators.pattern(/^\d+$/), this.positiveNumberValidator()]]
  });

  uploadForm = this.fb.group({
    excelFile: [''],
  });

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.expandedElement = null;
    this.deviceUnidentifiedService.getAllRecords().subscribe({
      next: (result) => {
        let totalResult: DeviceUnidentified[] = [];
        //Get unique values of all device types.
        let deviceTypes = result.map((r) => r.deviceTypeName).filter(this.valuesUnique);
        //Get the total amount for each device.
        deviceTypes.forEach((dtName) => {
          let uniqueDeviceTypeAmount = result.filter((r) => r.deviceTypeName === dtName).map((r) => r.amount)
          let totalAmount = uniqueDeviceTypeAmount.reduce((pre, cur) => pre + cur);
          let deviceUnidentifiedTotal = <DeviceUnidentified>{};
          deviceUnidentifiedTotal.deviceTypeName = dtName;
          deviceUnidentifiedTotal.deviceStatusName = 'Total';
          deviceUnidentifiedTotal.amount = totalAmount;
          totalResult.push(deviceUnidentifiedTotal);
        });

        //Project the 2 arrays into source tables.
        this.devicesUnidentified = new MatTableDataSource(result);
        this.devicesUnidentifiedTotal = new MatTableDataSource(totalResult);

        if (this.selectedElement) {
          this.expandedElement = totalResult.filter((d) => d.deviceTypeName === this.selectedElement!.deviceTypeName)[0];
          this.filterByDeviceType(this.expandedElement);
        }
      },
      error: (err: HttpErrorResponse) => {
        this.generalErrorMessage = err.message;
      }
    });
  }


  positiveNumberValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const amountValue: number = +control.value;
      return amountValue <= 0 ? { notPositiveNumber: true } : null;
    }
  }


  numberNotExceedCurrentValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const amountValue: number = +control.value;
      if (this.selectedElement) {
        return (this.selectedElement!.transferTypeId !== 1 && this.selectedElement!.amount < amountValue) ?
          { amountExceededCurrent: true } : null
      }
      return null;
    }
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


  filterByDeviceType(deviceTotalRecord: DeviceUnidentified): void {
    this.devicesUnidentified.filter = deviceTotalRecord.deviceTypeName;
  }

  valuesUnique(value: any, index: number, self: string[]): boolean {
    return self.indexOf(value) === index;
  }


  onSubmit(): void {
    this.baseService.setProcessingStatusValue(true);
    let deviceUnidentified = <DeviceUnidentified>{};
    deviceUnidentified.deviceStatusId = this.selectedElement!.deviceStatusId;
    deviceUnidentified.deviceTypeId = this.selectedElement!.deviceTypeId;
    deviceUnidentified.transferTypeId = this.selectedElement!.transferTypeId;
    deviceUnidentified.amount = +this.deviceUnidentifiedForm.controls['amount'].value!;

    this.deviceUnidentifiedService.sendInputData(deviceUnidentified).subscribe({
      next: () => {
        this.clearModal();
        this.loadData();
        this.baseService.setProcessingStatusValue(false);
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }

  downloadSampleAssignation(): void {
    let url = environment.baseUrl + 'api/import/assignation/unidentified';
    this.downloadService.downloadFile(url, "Sample_assignation_unidentified.xlsx");
  }

  onUploadCancel(): void {
    this.uploadForm.reset();
    this.progress = undefined;
    this.fileValid = false;
    this.uploadErrorMsg = undefined;
  }
  
  clearModal(): void {
    this.actionModal?.nativeElement.click();
    this.deviceUnidentifiedForm.reset();
  }

  getSelectedData(device: DeviceUnidentified, transferTypeId: number) {
    console.log(device);
    this.selectedElement = device;
    this.selectedElement.transferTypeId = transferTypeId;
    this.renderTitle(device.deviceTypeName, transferTypeId);
  }

  renderTitle(deviceTypeName: string, transferTypeId: number) {
    switch (transferTypeId) {
      case 1:
        this.title = `Add new ${deviceTypeName}s`;
        this.modalInstruction = `How many ${this.selectedElement!.deviceTypeName}(s) do you want to add?`;
        break;
      case 5:
        this.title = `Decommission ${deviceTypeName}s`;
        this.modalInstruction = `How many ${this.selectedElement!.deviceTypeName}(s) do you want to decommission?`;
        break;
      case 11:
        this.title = `Move ${deviceTypeName}s to Damaged List`;
        this.modalInstruction = `How many ${this.selectedElement!.deviceTypeName}(s) do you want to move to damaged list?`;
        break;
      default:
        break;
    }
  }

  onUploadAssignation(): void {
    this.progress = 0;
    this.fileProcessing = true;
    let formData = new FormData();
    formData.append('file', this.fileToUpload!, this.fileToUpload!.name);
    this.deviceUnidentifiedService.uploadDeviceUserUd(formData).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress) {
          this.progress = Math.round(100 * event.loaded / event.total!);
        } else if (event.type === HttpEventType.Response) {
          this.baseService.displayFileUploadSuccessPopup();
          this.uploadUserDeviceModalUd?.nativeElement.click();
          this.fileProcessing = false;
          this.loadData();
        }
      },
      error: (error: HttpErrorResponse) => {
        console.log(error.error);
        if (error.error.length > 1) {
          this.uploadErrorMsg = error.error.split(`\n`);
          this.fileProcessing = false;
        }
        else {
          this.uploadErrorMsg?.push(error.message);
        }
      }
    });
  }
  
  downloadUdReport(): void {
    let url = environment.baseUrl + 'api/download/devicesud';
    this.downloadService.downloadFile(url, "report-unidentified.xlsx");
  }

}
