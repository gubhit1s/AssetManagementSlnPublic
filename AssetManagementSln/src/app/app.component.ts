import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth/auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  isLoggedIn: boolean = false;
  
  title = "AssetManagement";

  constructor(private authService: AuthService, private router: Router) {
  }

  ngOnInit(): void {
    this.authService.authStatus.subscribe((r) => this.isLoggedIn = r);
    this.authService.init();
  }
  

}
