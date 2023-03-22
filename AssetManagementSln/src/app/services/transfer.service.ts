import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Transfer } from '../modules/transfer';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PendingTransfer } from '../modules/pending-transfer';

@Injectable({
  providedIn: 'root'
})
export class TransferService {
  
  

  constructor(private http: HttpClient) { }

  getAllCompletedTransfers(fromDate: string, toDate: string): Observable<Transfer[]> {
    let url = environment.baseUrl + 'api/transferhistory/completed/identified';
    let params = new HttpParams()
      .set("fromDate", fromDate)
      .set("toDate", toDate);
    return this.http.get<Transfer[]>(url, { params });
  }

  getAllCompletedTransfersUd(fromDate: string, toDate: string): Observable<Transfer[]> {
    let url = environment.baseUrl + 'api/transferhistory/completed/unidentified';
    let params = new HttpParams()
      .set("fromDate", fromDate)
      .set("toDate", toDate);
    return this.http.get<Transfer[]>(url, { params });
  }

  getAllPendingTransfers(): Observable<PendingTransfer[]> {
    let url = environment.baseUrl + 'api/transferhistory/pending';
    return this.http.get<PendingTransfer[]>(url);
  }

}
