import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { LocationStrategy, HashLocationStrategy } from '@angular/common';

import { MatTableModule } from '@angular/material/table';
import { FormsModule } from '@angular/forms';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatInputModule } from '@angular/material/input';
import { ReactiveFormsModule } from '@angular/forms';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCardModule } from '@angular/material/card';
import { MatRadioModule } from '@angular/material/radio';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCheckboxModule } from '@angular/material/checkbox';


import { AppComponent } from './app.component';
import { VerNavBarComponent } from './ver-nav-bar/ver-nav-bar.component';
import { AppRoutingModule } from './app-routing.module';
import { DeviceTypesComponent } from './settings/device-types/device-types.component';
import { HomeComponent } from './home/home.component';
import { DevicesComponent } from './devices/devices.component';
import { UsersComponent } from './users/users.component';
import { DeviceAssignComponent } from './devices/device-assign/device-assign.component';
import { DeviceConfirmComponent } from './device-confirm/device-confirm.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { DeviceInformationComponent } from './devices/device-information/device-information.component';
import { AppAnnouncementComponent } from './app-tools/app-announcement/app-announcement.component';
import { AppLoadingComponent } from './app-tools/app-loading/app-loading.component';
import { DevicesListComponent } from './devices/devices-list/devices-list.component';
import { UserDevicesComponent } from './users/user-devices/user-devices.component';
import { EmailManagementComponent } from './settings/email-management/email-management.component';
import { LoginComponent } from './auth/login.component';
import { DevicesUnidentifiedComponent } from './devices/devices-unidentified.component';
import { DeviceAssignUnidentifiedComponent } from './devices/device-assign/device-assign-unidentified.component';
import { AppReloadComponent } from './app-tools/app-reload/app-reload.component';
import { BatchTransferComponent } from './users/batch-transfer/batch-transfer.component';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { CompletedTransfersComponent } from './transfer-history/completed-transfers/completed-transfers.component';
import { AlertErrorComponent } from './app-tools/alert-error/alert-error.component';
import { AuthInterceptor } from './auth/auth.interceptor';
import { PermissionComponent } from './settings/permission/user-permissions/permission.component';
import { PendingTransfersComponent } from './transfer-history/pending-transfers/pending-transfers.component';
import { RolesPermissionsComponent } from './settings/permission/roles-permissions.component';
import { RolesConfigurationComponent } from './settings/permission/roles-configuration/roles-configuration.component';


@NgModule({
  declarations: [
    AppComponent,
    VerNavBarComponent,
    DeviceTypesComponent,
    HomeComponent,
    DevicesComponent,
    UsersComponent,
    DeviceAssignComponent,
    DeviceConfirmComponent,
    DeviceInformationComponent,
    AppAnnouncementComponent,
    AppLoadingComponent,
    DevicesListComponent,
    UserDevicesComponent,
    EmailManagementComponent,
    LoginComponent,
    DevicesUnidentifiedComponent,
    DeviceAssignUnidentifiedComponent,
    AppReloadComponent,
    BatchTransferComponent,
    CompletedTransfersComponent,
    AlertErrorComponent,
    PermissionComponent,
    PendingTransfersComponent,
    RolesPermissionsComponent,
    RolesConfigurationComponent,

  ],
  imports: [
    BrowserModule, HttpClientModule, AppRoutingModule, CommonModule,
    MatTableModule, FormsModule, MatPaginatorModule, MatSortModule, MatInputModule,
    ReactiveFormsModule, BrowserAnimationsModule, MatProgressBarModule,
    MatTooltipModule, MatCardModule, MatRadioModule, MatIconModule,
    MatButtonToggleModule, MatBadgeModule, MatSnackBarModule, MatTabsModule,
    MatCheckboxModule
  ],
  exports: [],
  providers: [{ provide: LocationStrategy, useClass: HashLocationStrategy },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
