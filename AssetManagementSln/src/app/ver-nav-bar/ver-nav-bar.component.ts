import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-ver-nav-bar',
  templateUrl: './ver-nav-bar.component.html',
  styleUrls: ['./ver-nav-bar.component.scss']
})
export class VerNavBarComponent {

  @ViewChild('logoutModal') logoutModal?: ElementRef;
  @ViewChild('dashboard') dashboardBtn?: ElementRef;

  deviceExpand: boolean = true;
  transferExpand: boolean = false;
  settingExpand: boolean = false;


  setActive(event: Event, className: string): void {
    let preActiveEl = document.getElementsByClassName("active")[0];
    preActiveEl.classList.remove("active");
    let target = event.target as HTMLInputElement;
    target?.classList.add(className);
  }

  constructor(private authService: AuthService, private router: Router) {
  }

  onLogout(): void {
    this.logoutModal?.nativeElement.click();
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  setActiveDashboard(): void {
    let preActiveEl = document.getElementsByClassName("active")[0];
    preActiveEl.classList.remove("active");
    let dashBoard = this.dashboardBtn?.nativeElement as HTMLInputElement;
    dashBoard.classList.add("active");
  }

  toggleDevice(): void {
    this.deviceExpand = !this.deviceExpand;
  }

  toggleTransfer(): void {
    this.transferExpand = !this.transferExpand;
  }

  toggleSetting(): void {
    this.settingExpand = !this.settingExpand;
  }

}
