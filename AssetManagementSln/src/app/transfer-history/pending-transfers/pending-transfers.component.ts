import { Component, OnInit } from '@angular/core';
import { TransferService } from 'src/app/services/transfer.service';
import { PendingTransfer } from 'src/app/modules/pending-transfer';
import { MatTableDataSource } from '@angular/material/table';
import { BaseService } from 'src/app/base.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-pending-transfers',
  templateUrl: './pending-transfers.component.html',
  styleUrls: ['./pending-transfers.component.css']
})
export class PendingTransfersComponent extends BaseService implements OnInit {

  title: string = 'Pending Transfers';

  displayedColumns: string[] = ['deviceType', 'serviceTag', 'userName', 'transferType', 'expiryDate'];

  pendingTransfers!: MatTableDataSource<PendingTransfer>;

  constructor(private transferService: TransferService) { 
    super();
  }

  ngOnInit(): void {

    this.loadData();
  }

  loadData(): void {
    this.transferService.getAllPendingTransfers().subscribe({
      next: (result) => {
        this.pendingTransfers = new MatTableDataSource(result);
      },
      error: (err: HttpErrorResponse) => {
        this.generalErrorMessage = err.message;
      },
    });
  }

}
