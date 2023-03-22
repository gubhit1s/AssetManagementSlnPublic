import { Component, OnInit, AfterViewInit, Input, Output, EventEmitter, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { User } from '../modules/user';
import { MatRow, MatRowDef, MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { BaseService } from '../base.service';
import { TooltipPosition } from '@angular/material/tooltip';
import { MatPaginator } from '@angular/material/paginator';
import { UserService } from '../services/user.service';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})
export class UsersComponent extends BaseService implements OnInit {

  tooltipAbove: TooltipPosition = 'above';

  displayedColumns: string[] = ['userName', 'displayName', 'email', 'department', 'office', 'actions'];

  searchText?: string;

  @Input() title: string = "List of Users";

  userSelected?: User | null;

  public users!: MatTableDataSource<User>;

  //Create an EventEmitter instance to record the selected row when reusing this component.
  @Output() select = new EventEmitter<User>();

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(private http: HttpClient, private baseService: BaseService,
    private userService: UserService
  ) { 
    super();
  }

  ngOnInit(): void {
    let url = environment.baseUrl + 'api/users';
    this.userService.getAllActiveUsers().subscribe({
      next: (result) => {
        this.users = new MatTableDataSource<User>(result);
        this.users.sort = this.sort;
        this.users.paginator = this.paginator;
      },
      error: (error) => {
        console.log(error);
      }
    });
  }

  highlight(user: User): void {
    this.userSelected = user
    this.select.emit(user);
  }


}
