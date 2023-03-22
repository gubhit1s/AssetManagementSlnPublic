import { Component, OnInit, ViewChild, ElementRef, Input } from '@angular/core';
import { BaseService } from 'src/app/base.service';
import { PermissionService } from 'src/app/services/permission.service';
import { MatTableDataSource } from '@angular/material/table';
import { User } from 'src/app/modules/user';
import { HttpErrorResponse } from '@angular/common/http';
import { MatSort } from '@angular/material/sort';
import { UserService } from 'src/app/services/user.service';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { STRING_TYPE } from '@angular/compiler';

@Component({
  selector: 'app-permission',
  templateUrl: './permission.component.html',
  styleUrls: ['./permission.component.css']
})
export class PermissionComponent extends BaseService implements OnInit {

  public permittedUsers!: MatTableDataSource<User>;
  public allUsers!: MatTableDataSource<User>;
  permittedUsersArr?: User[];
  allUsersArr?: User[];
  checkedUsersToGrant?: User[];
  checkedUsersToRevoke?: User[];
  selectedUser?: User;

  customizedError?: string;

  public displayedColumns: string[] = ['userName', 'displayName', 'email', 'actions'];
  public displayedColumnsAllUsers: string[] = ['checkbox', 'userName', 'displayName', 'email'];

  allChecked: boolean = false;

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild('permissionModal') permissionModal!: ElementRef;
  @ViewChild('confirmRevokeModal') confirmRevokeModal!: ElementRef;

  constructor(private baseService: BaseService,
    private permissionService: PermissionService,
    private userService: UserService
  ) {
    super();
  }

  ngOnInit(): void {
    this.loadPermittedUsers();
  }

  loadPermittedUsers(): void {
    this.permissionService.getAllPermittedUsers().subscribe({
      next: (result) => {
        this.permittedUsers = new MatTableDataSource<User>(result);
        this.permittedUsers.sort = this.sort;
        this.permittedUsersArr = result;
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
      }
    });
  }

  loadUsers(): void {
    this.userService.getAllActiveUsers().subscribe({
      next: (result) => {
        // Add checkboxes as booleans to the result.
        
        this.allUsersArr = result;
        this.allUsers = new MatTableDataSource<User>(result);
        this.allUsers.sort = this.sort;
        this.allUsers.paginator = this.paginator;
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.generalErrorMessage = err.message;
      }
    });
  }

  updateAllChecked(userArr: User[]): void {
    this.allChecked = userArr != null && userArr.every(u => u.checked);
  }

  someChecked(userArr: User[]): boolean {
    if (userArr == null) {
      return false;
    }
    return userArr.filter(u => u.checked).length > 0 && !this.allChecked;
  }

  setAll(checked: boolean, userArr: User[]): void {
    this.allChecked = checked;
    if (userArr == null) {
      return;
    }
    userArr.forEach(u => u.checked = checked);
  }

  getCheckedUsersToGrant(): void {
    let checkedUsersToGrant = this.allUsersArr?.filter(u => u.checked == true);
    this.checkedUsersToGrant = checkedUsersToGrant;
  }

  getCheckedUsersToRevoke(): void {
    let checkedUsersToRevoke = this.allUsersArr?.filter(u => u.checked == true);
    this.checkedUsersToRevoke = checkedUsersToRevoke;
  }

  grantAccess(): void {
    this.baseService.setProcessingStatusValue(true);
    console.log(this.checkedUsersToGrant);
    if (!this.checkedUsersToGrant) {
      console.log("No user to process!");
      return;
    }
    this.permissionService.grantAccessToUsers(this.checkedUsersToGrant).subscribe({
      next: () => {
        this.permissionModal.nativeElement.click();
        this.baseService.displayPermissionGrantedPopup();
        this.baseService.setProcessingStatusValue(false);
        this.loadPermittedUsers();
      },
      error: (err: HttpErrorResponse) => {
        console.log(err.error);
        this.customizedError = err.error;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }

  
  getSelectedUser(user: User): void {
    this.selectedUser = user
  }

  revokeAccess(): void {
    this.baseService.setProcessingStatusValue(true);
    if (!this.selectedUser) {
      throw new Error('Cannot find user to revoke access.');
    }

    this.permissionService.deleteUserFromIdentityData(this.selectedUser.userName).subscribe({
      next: () => {
        this.confirmRevokeModal.nativeElement.click();
        this.baseService.setProcessingStatusValue(false);
        this.loadPermittedUsers();
      },
      error: (err: HttpErrorResponse) => {
        this.generalErrorMessage = err.message;
        if (typeof (err.error) === 'string') this.generalErrorMessage += ': ' + err.error;
        this.baseService.setProcessingStatusValue(false);
      }
    });
  }


}
