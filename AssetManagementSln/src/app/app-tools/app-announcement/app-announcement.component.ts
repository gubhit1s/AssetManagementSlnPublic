import { Component, OnInit, Input } from '@angular/core';
import { BaseService } from '../../base.service';
import { transition, style, animate, trigger } from '@angular/animations';

const enterTransition = transition(':enter', [
  style({
    opacity: 0
  }),
  animate('1s ease-in', style({
    opacity: 1
  }))
]);

const leaveTransition = transition(':leave', [
  style({
    opacity: 1
  }),
  animate('3s ease-out', style({
    opacity: 0
  }))
]);

const fadeIn = trigger('fadeIn', [
  enterTransition
]);

const fadeOut = trigger('fadeOut', [
  leaveTransition
]);


@Component({
  selector: 'app-announcement',
  templateUrl: './app-announcement.component.html',
  styleUrls: ['./app-announcement.component.css'],
  animations: [
    fadeIn,
    fadeOut
  ]
})
export class AppAnnouncementComponent implements OnInit {

  public flag: number = 0;

  public message?: string;

  public show: boolean = false;

  constructor(private baseService: BaseService) { }

  ngOnInit(): void {
    this.baseService.getProcessedStatusValue().subscribe((value) => {
      this.flag = value;
      this.show = true;
    });
  }

  closePopup(): void {
    this.show = false;
  }

}
