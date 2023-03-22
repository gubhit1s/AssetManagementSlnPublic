import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { Device } from '../../modules/device';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-device-information',
  templateUrl: './device-information.component.html',
  styleUrls: ['./device-information.component.css']
})
export class DeviceInformationComponent implements OnInit {

  generalErrorMessage?: string;

  id?: number;

 // @Input() deviceFromParent?: Device;

  @Input() device?: Device;

  @Output() loaded: EventEmitter<Device> = new EventEmitter<Device>();

  constructor(private http: HttpClient, private route: ActivatedRoute) { }

  ngOnInit(): void {
    let id = this.route.snapshot.paramMap.get('id');
    this.id = id ? +id : 0;
    let url = environment.baseUrl + 'api/devices/' + id;

    this.http.get<Device>(url).subscribe({
      next: (result) => {
        console.log(result);
        this.device = result;
        this.loaded.emit(result);
      },
      error: (error: HttpErrorResponse) => {
        console.log(error);
        this.generalErrorMessage = error.message;
      }
    });
  }

}
