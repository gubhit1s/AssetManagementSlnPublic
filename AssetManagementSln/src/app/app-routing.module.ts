import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { VerNavBarComponent } from './ver-nav-bar/ver-nav-bar.component';
import { DeviceTypesComponent } from './settings/device-types/device-types.component';
import { HomeComponent } from './home/home.component';
import { DevicesComponent } from './devices/devices.component';
import { UsersComponent } from './users/users.component';
import { DeviceAssignComponent } from './devices/device-assign/device-assign.component';
import { DeviceConfirmComponent } from './device-confirm/device-confirm.component';
import { DeviceInformationComponent } from './devices/device-information/device-information.component';
import { CompletedTransfersComponent } from './transfer-history/completed-transfers/completed-transfers.component';
import { UserDevicesComponent } from './users/user-devices/user-devices.component';
import { EmailManagementComponent } from './settings/email-management/email-management.component';
import { LoginComponent } from './auth/login.component';
import { AuthGuard } from './auth/auth-guard';
import { DevicesUnidentifiedComponent } from './devices/devices-unidentified.component';
import { DeviceAssignUnidentifiedComponent } from './devices/device-assign/device-assign-unidentified.component';
import { BatchTransferComponent } from './users/batch-transfer/batch-transfer.component';
import { PendingTransfersComponent } from './transfer-history/pending-transfers/pending-transfers.component';
import { RolesPermissionsComponent } from './settings/permission/roles-permissions.component';


const routes: Routes = [
  { path: '', component: HomeComponent, pathMatch: 'full', canActivate: [AuthGuard] },
  { path: 'devicetypes', component: DeviceTypesComponent, canActivate: [AuthGuard] },
  { path: 'devices', component: DevicesComponent, canActivate: [AuthGuard] },
  { path: 'devices/unidentified', component: DevicesUnidentifiedComponent, canActivate: [AuthGuard] },
  { path: 'users', component: UsersComponent, canActivate: [AuthGuard] },
  { path: 'users/:user', component: UserDevicesComponent, canActivate: [AuthGuard] },
  { path: 'users/batch/:user', component: BatchTransferComponent, canActivate: [AuthGuard] },
  { path: 'device/assign/:id', component: DeviceAssignComponent },
  { path: 'device/assign', component: DeviceAssignComponent, canActivate: [AuthGuard] },
  { path: 'device/unidentified/assign/:id', component: DeviceAssignUnidentifiedComponent, canActivate: [AuthGuard] },
  { path: 'device/confirm', component: DeviceConfirmComponent },
  { path: 'device/unidentified/confirm', component: DeviceConfirmComponent },
  { path: 'device/bulk/confirm', component: DeviceConfirmComponent },
  { path: 'device/:id', component: DeviceInformationComponent, canActivate: [AuthGuard] },
  { path: 'transferhistory/completed', component: CompletedTransfersComponent, canActivate: [AuthGuard] },
  { path: 'transferhistory/pending', component: PendingTransfersComponent, canActivate: [AuthGuard] },
  { path: 'emailsettings', component: EmailManagementComponent, canActivate: [AuthGuard] },
  { path: 'permission', component: RolesPermissionsComponent, canActivate: [AuthGuard] },
  { path: 'login', component: LoginComponent },
]

@NgModule({
  declarations: [],
  imports: [
    RouterModule.forRoot(routes, {
      scrollPositionRestoration: 'enabled'
    })
  ],
  exports: [RouterModule]
})
export class AppRoutingModule { }

