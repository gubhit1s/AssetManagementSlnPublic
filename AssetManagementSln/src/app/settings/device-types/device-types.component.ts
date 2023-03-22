import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DeviceType } from '../../modules/device-type';
import { MatTableDataSource } from '@angular/material/table';
import { environment } from 'src/environments/environment';
import { Router } from '@angular/router';
import { NgForm } from '@angular/forms';
import { Device } from '../../modules/device';
import { BaseService } from '../../base.service';
import { DeviceTypeService } from 'src/app/services/device-type.service';

@Component({
  selector: 'app-device-types',
  templateUrl: './device-types.component.html',
  styleUrls: ['./device-types.component.css']
})
export class DeviceTypesComponent implements OnInit {

  data!: string;

  displayedColumns: string[] = ['id', 'name', 'action'];

  errorAddMessage?: string;

  errorDeleteMessage?: string;

  identified: boolean = true;

  identifiedInput: boolean = true;

  selectedDeviceType?: DeviceType;

  url: string = environment.baseUrl + 'api/DeviceTypes';

  @ViewChild('addModal') addModal?: ElementRef;
  @ViewChild('deleteModal') deleteModal?: ElementRef;
  @ViewChild('deleteBtn') deleteBtn?: ElementRef;

  public deviceTypesIdentified!: MatTableDataSource<DeviceType>;
  public deviceTypesUnidentified!: MatTableDataSource<DeviceType>;

  constructor(
    private http: HttpClient,
    private router: Router,
    private baseService: BaseService,
    private deviceTypeSerivce: DeviceTypeService
  ) { }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.deviceTypeSerivce.getAllDeviceTypes().subscribe({
      next: (result) => {
        let identifiedList = result.filter((dt) => dt.isIdentified);
        let unidentifiedList = result.filter((dt) => !dt.isIdentified);
        this.deviceTypesIdentified = new MatTableDataSource<DeviceType>(identifiedList);
        this.deviceTypesUnidentified = new MatTableDataSource<DeviceType>(unidentifiedList);
      },
      error: (error) => {
        console.log(error);
      }
    });
  }

  onSubmit(dtForm: NgForm): void {
    this.baseService.setProcessingStatusValue(true);
    let deviceType = <DeviceType>{};
    deviceType.name = this.data;
    deviceType.isIdentified = this.identifiedInput;
    this.http.post<DeviceType>(this.url, deviceType).subscribe({
      next: (result) => {
        console.log("Type " + result.name + " has been updated");
        this.addModal?.nativeElement.click();
        dtForm.form.reset();
        this.router.navigate(['/devicetypes']);
        this.loadData();
        this.baseService.setProcessingStatusValue(false);
      },
      error: (error: HttpErrorResponse) => {
        console.log(error);
        this.errorAddMessage = error.message;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }

  onDelete(): void {
    let id = this.selectedDeviceType?.id;
    console.log(id);
    let idUrl = this.url + '/' + id;
    this.http.delete<DeviceType>(idUrl).subscribe({
      next: result => {
        console.log(result);
        this.deleteModal?.nativeElement.click();
        this.router.navigate(['/devicetypes']);
        this.loadData();
      },
      error: (error: HttpErrorResponse) => {
        console.log(error);
        this.errorDeleteMessage = error.error;
      }
    });
  }

  onModalClose() {
    this.errorAddMessage = '';
    this.errorDeleteMessage = '';
  }

  setIdentified(event: boolean): void {
    this.identified = event;
  }

  getDeviceType(deviceType: DeviceType): void {
    this.selectedDeviceType = deviceType;
    console.log(this.selectedDeviceType);
  }
}
