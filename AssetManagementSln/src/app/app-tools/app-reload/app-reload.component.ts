import { Component, AfterViewInit, ViewEncapsulation, ViewChild, ElementRef } from '@angular/core';

import { TooltipPosition } from '@angular/material/tooltip';

@Component({
  selector: 'app-reload',
  templateUrl: './app-reload.component.html',
  styleUrls: ['./app-reload.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class AppReloadComponent implements AfterViewInit {
  tooltipAbove: TooltipPosition = 'above';

  @ViewChild('tooltip') toolTip?: ElementRef;

  constructor() { }

  ngAfterViewInit(): void {
  }

  reload(): void {
    window.location.reload();
  }

}
