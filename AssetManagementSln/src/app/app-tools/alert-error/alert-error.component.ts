import { Component, OnInit, Input } from '@angular/core';
import { BaseService } from 'src/app/base.service';

@Component({
  selector: 'app-alert-error',
  templateUrl: './alert-error.component.html',
  styleUrls: ['./alert-error.component.css']
})
export class AlertErrorComponent implements OnInit {

  @Input() errorMessage: string = "Some Error";

  constructor() {
  }

  ngOnInit(): void {
  }

}
