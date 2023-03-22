import { Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { BaseService } from '../base.service';
import { HomeService } from '../services/home.service';
import { DeviceOverview } from '../modules/device-overview';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent extends BaseService implements OnInit {

  widthInStock: number = 0;
  widthInUse: number = 0;

  deviceOverviews?: DeviceOverview[];

  constructor(private baseService: BaseService,
    private homeService: HomeService
  ) { 
    super();
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.homeService.getDeviceOverviews().subscribe({
      next: (result) => {
        result.forEach((deviceOverview) => {
          deviceOverview.inStockPercentage = (deviceOverview.inStockAmount / deviceOverview.totalAmount) * 100;
          deviceOverview.inUsePercentage = (deviceOverview.inUseAmount / deviceOverview.totalAmount) * 100;
        });
        this.deviceOverviews = result;
      },
      error: (err: HttpErrorResponse) => {
        this.generalErrorMessage = err.message;
        console.log(err.error);
      }
    });
  }

}
