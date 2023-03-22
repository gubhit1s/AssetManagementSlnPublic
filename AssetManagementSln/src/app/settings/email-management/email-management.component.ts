import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators, FormBuilder } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { BaseService } from '../../base.service';
import { EmailAdmin } from './email-admin';

@Component({
  selector: 'app-email-management',
  templateUrl: './email-management.component.html',
  styleUrls: ['./email-management.component.css']
})
export class EmailManagementComponent extends BaseService implements OnInit {

  emailAdForm!: FormGroup;

  currentEmailSettings?: EmailAdmin;

  url = environment.baseUrl + 'api/email';

  constructor(private formBuilder: FormBuilder, private http: HttpClient, private baseService: BaseService) { 
    super();
  }

  ngOnInit(): void {  
    this.emailAdForm = this.formBuilder.group({
      emailAdmin: ['', [Validators.required, Validators.pattern(/^[_A-Za-z0-9.]+\@tmf-group\.com$/)]],
      smtpHost: ['', [Validators.required, Validators.pattern(/^([0-9]+\.){3}[0-9]+$/)]],
      smtpPort: ['', [Validators.required, Validators.pattern(/^[0-9]+$/)]],
      emailFrom: ['', [Validators.required, Validators.pattern(/^[_A-Za-z0-9.]+\@tmf-group\.com$/)]],
      titleFrom: ['', Validators.required],
      enableSSL: ['']
    });
    this.loadData();
  }

  loadData(): void {
    this.http.get<EmailAdmin>(this.url).subscribe({
      next: (result) => {
        this.currentEmailSettings = result;
        this.emailAdForm.patchValue(result);
      },
      error: (error: HttpErrorResponse) => {
        console.log(error.error);
        this.generalErrorMessage = error.message;
      }
    });
  }


  onSubmit(): void {
    this.baseService.setProcessingStatusValue(true);
    let updatedSetting = <EmailAdmin>{};
    updatedSetting.emailAdmin = this.emailAdForm.controls['emailAdmin'].value;
    updatedSetting.smtpHost = this.emailAdForm.controls['smtpHost'].value;
    updatedSetting.smtpPort = this.emailAdForm.controls['smtpPort'].value;
    updatedSetting.emailFrom = this.emailAdForm.controls['emailFrom'].value;
    updatedSetting.titleFrom = this.emailAdForm.controls['titleFrom'].value;
    updatedSetting.enableSsl = this.emailAdForm.controls['enableSSL'].value;
    console.log(updatedSetting);

    this.http.put<EmailAdmin>(this.url, updatedSetting).subscribe({
      next: () => {
        this.baseService.displayNotification(10);
        this.baseService.setProcessingStatusValue(false);
      },
      error: (error: HttpErrorResponse) => {
        console.log(error.error);
        this.generalErrorMessage = error.message;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }

}
