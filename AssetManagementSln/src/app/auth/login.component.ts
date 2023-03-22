import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup, FormControl, Validators, AbstractControl, AsyncValidatorFn, FormBuilder } from '@angular/forms';
import { AuthService } from './auth.service';
import { UserForAuthentication } from './user-for-authentication';
import { LoginResult } from './login-result';
import { User } from '../modules/user';
import { BaseService } from '../base.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent extends BaseService implements OnInit {
  loginResult?: LoginResult;
  loginForm!: FormGroup;

  constructor(private activatedRoute: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private formBuilder: FormBuilder,
    private baseService: BaseService
  ) { 
    super();
  }

  ngOnInit(): void {
    this.loginForm = this.formBuilder.group({
      userName: new FormControl('', Validators.required),
      password: new FormControl('', Validators.required)
    });
    
  }

  onSubmit(): void {
    this.baseService.setProcessingStatusValue(true);
    let loginRequst = <UserForAuthentication>{};
    loginRequst.userName = this.loginForm.controls['userName'].value;
    loginRequst.password = this.loginForm.controls['password'].value;

    this.authService.login(loginRequst).subscribe({
      next: (result) => {
        this.loginResult = result;
        if (result.success && result.token) {
          this.router.navigate(["/"]);
        }
        this.baseService.setProcessingStatusValue(false);
      },
      error: (error: HttpErrorResponse) => {
        console.log(error.error);
        this.generalErrorMessage = error.error;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }
      
}
