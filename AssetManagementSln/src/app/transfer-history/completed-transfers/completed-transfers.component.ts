import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { MatTableDataSource } from '@angular/material/table';
import { MatSort, Sort } from '@angular/material/sort';
import { environment } from 'src/environments/environment';
import { Transfer } from 'src/app/modules/transfer';
import { BaseService } from 'src/app/base.service';
import { FormBuilder, Validators } from '@angular/forms';
import { TransferService } from 'src/app/services/transfer.service';
import { last } from 'rxjs';
import { DeviceUnidentified } from 'src/app/modules/device-unidentified';
import { MatPaginator } from '@angular/material/paginator';

@Component({
  selector: 'app-completed-transfers',
  templateUrl: './completed-transfers.component.html',
  styleUrls: ['./completed-transfers.component.css']
})
export class CompletedTransfersComponent extends BaseService implements OnInit {

  public displayedColumns: string[] = ['transferDate', 'deviceServiceTag', 'deviceTypeName', 'transferFrom', 'transferTo', 'transferTypeName'];
  public displayedColumnsUd: string[] = ['transferDate', 'deviceTypeName', 'transferFrom', 'transferTo', 'amount', 'transferTypeName'];

  public transfers!: MatTableDataSource<Transfer>;
  public transfersUd!: MatTableDataSource<Transfer>;

  public title: string = "Completed Transfers";

  public identified: boolean = true;

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild('submitButton') submitButton!: ElementRef;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  constructor(private transferService: TransferService, private fb: FormBuilder) { 
    super();
  }

  
  filterDateForm = this.fb.group({
    from: ['', Validators.required],
    to: ['', Validators.required]
  });

  ngOnInit(): void {
    let today = new Date();
    let lastMonth = new Date();
    lastMonth.setMonth(lastMonth.getMonth() - 1);
    let initialData = { from: this.toDefaultDateString(lastMonth), to: this.toDefaultDateString(today) }; //Input dates receive format: yyyy-MM-dd
    this.filterDateForm.setValue(initialData);
    this.loadData();
  }

  loadData(): void {
    let fromDate = this.filterDateForm.controls['from'].value!;
    let toDate = this.filterDateForm.controls['to'].value!;
    if (this.identified)
      this.transferService.getAllCompletedTransfers(fromDate, toDate).subscribe({
        next: (result) => {
          this.transfers = new MatTableDataSource<Transfer>(result);
          this.transfers.sort = this.sort;
          this.transfers.paginator = this.paginator;
          this.submitButton.nativeElement.blur();
        },
        error: (error: HttpErrorResponse) => {
          console.log(error);
          this.generalErrorMessage = error.message;
        }
      });
    else {
      this.transferService.getAllCompletedTransfersUd(fromDate, toDate).subscribe({
        next: (result) => {
          this.transfersUd = new MatTableDataSource<Transfer>(result);
          this.transfersUd.sort = this.sort;
          this.transfersUd.paginator = this.paginator;
          this.submitButton.nativeElement.blur();
        },
        error: (error: HttpErrorResponse) => {
          console.log(error);
          this.generalErrorMessage = error.message;
        }
      });
    }
  }


  toDefaultDateString(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  setIdentified(event: boolean): void {
    this.identified = event;
    this.loadData();
  }

}
