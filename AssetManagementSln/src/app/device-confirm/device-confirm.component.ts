import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from 'src/environments/environment';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { BaseService } from '../base.service';
import { ConfirmationToken } from '../modules/confirmation-token';
import { NgForm } from '@angular/forms';

@Component({
  selector: 'app-device-confirm',
  templateUrl: './device-confirm.component.html',
  styleUrls: ['./device-confirm.component.css']
})
export class DeviceConfirmComponent implements OnInit {

  constructor(private route: ActivatedRoute, private http: HttpClient, private baseService: BaseService,
    private router: Router
  ) { }

  tokenProcessed: boolean = false; 
  completed: boolean = false;
  showForm: boolean = false;
  
  msg: string = "Verifying, please wait for a moment...";
  url: string = this.getUrl();

  errorMsg?: string;
  declinedReason?: string | null;

  token: string | null = this.route.snapshot.queryParamMap.get('token');
  userConfirmed: boolean = (this.route.snapshot.queryParamMap.get('userConfirmed') == 'true');

  getUrl(): string {
    let thisUrl = this.router.url;
    let url = environment.baseUrl + 'api/';
    if (thisUrl.includes('unidentified')) {
      url = url + 'device/unidentified/confirm';
    } else if (thisUrl.includes('bulk')) {
      url = url + 'bulk/confirm';
    } else {
      url = url + 'device/confirm';
    }
    return url;
  }

  ngOnInit(): void {
    //Check if the token exists
    let urlToken = this.url + '?token=' + this.token;
    this.http.get(urlToken).subscribe({
      next: () => {
        if (!this.userConfirmed) {
          this.showForm = true;
          this.tokenProcessed = true;
        }
      },
      error: (error: HttpErrorResponse) => {
        console.log(error.error);
        this.errorMsg = "Error: Cannot process this url. It could have been expired. Please contact IT for support."; 
      }
    });

    //Get the token send it to the API.
    if (this.userConfirmed && !this.tokenProcessed) {
      this.tokenProcessed = false;
      let tokenInfo = <ConfirmationToken>{
        token: this.token,
        userConfirmed: this.userConfirmed
      };
      let verifyUrl = this.url + "/verify";
      this.http.post(verifyUrl, tokenInfo).subscribe({
        next: (result) => {
          this.tokenProcessed = true;
          this.completed = true;
          console.log(result);
        },
        error: (error: HttpErrorResponse) => {
          console.log(error.error);
          this.errorMsg = "Error: Cannot process this url. It could have been expired. Please contact IT for support."; 
        }
      });
    }
  }

  onSubmit(): void {
    this.tokenProcessed = false;
    this.showForm = false;
    let verifyUrl = this.url + "/verify";
    let tokenInfo = <ConfirmationToken>{
      token: this.token,
      userConfirmed: this.userConfirmed,
      declinedReason: this.declinedReason
    }

    this.http.post(verifyUrl, tokenInfo).subscribe({
      next: () => {
        this.tokenProcessed = true;
        this.completed = true;
      },
      error: (error: HttpErrorResponse) => {
        console.log(error.error);
      }
    });
  }

}
