import { Component, OnInit } from '@angular/core';
import { BaseService } from '../../base.service';

@Component({
  selector: 'app-loading',
  templateUrl: './app-loading.component.html',
  styleUrls: ['./app-loading.component.css']
})
export class AppLoadingComponent implements OnInit {

  public processing: boolean = false;

  constructor(private baseService: BaseService) { 
  }

  ngOnInit(): void {
    this.baseService.getProcessingStatusValue().subscribe((value) => {
      this.processing = value;
    });
  }

  

}
