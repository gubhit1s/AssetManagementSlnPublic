import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { MatTableDataSource } from '@angular/material/table';
import { AbstractControl } from '@angular/forms';

@Injectable({
  providedIn: 'root'
})
export class BaseService {

  private routerInfoProcessedStatus: BehaviorSubject<number>;
  private routerInfoProcessingStatus: BehaviorSubject<boolean>;

  public generalErrorMessage?: string;

  constructor() {
    this.routerInfoProcessedStatus = new BehaviorSubject<number>(0);
    this.routerInfoProcessingStatus = new BehaviorSubject<boolean>(false);
  }

  getProcessedStatusValue(): Observable<number> {
    return this.routerInfoProcessedStatus.asObservable();
  }

  setProcessedStatusValue(newValue: number): void {
    this.routerInfoProcessedStatus.next(newValue);
  }

  getProcessingStatusValue(): Observable<boolean> {
    return this.routerInfoProcessingStatus.asObservable();
  }

  setProcessingStatusValue(newValue: boolean): void {
    this.routerInfoProcessingStatus.next(newValue);
  }


  async displayEmailSentSuccessPopup() {
    await this.displayNotification(1);
  }

  async displayDecommissioningSuccessPopup() {
    await this.displayNotification(3);
  }

  async displayFileUploadSuccessPopup() {
    await this.displayNotification(4);
  }

  async displayRepairingSuccessPopup() {
    await this.displayNotification(5);
  }

  async displayMoveBackToStockSuccessPopup() {
    await this.displayNotification(6);
  }

  async displayExpiredSessionPopup() {
    await this.displayNotification(8);
  }

  async displayPermissionGrantedPopup() {
    await this.displayNotification(9);
  }
  
  async displayNotification(messageId: number) {
    this.setProcessedStatusValue(messageId);
    await new Promise(resolve => setTimeout(resolve, 1500));
    this.setProcessedStatusValue(0);
  }

  applyFilter<T>(event: Event, data: MatTableDataSource<T>) {
    event.preventDefault();
    let filterValue = (event.target as HTMLInputElement).value;
    data.filter = filterValue.trim().toLowerCase();
  }

  getErrors(control: AbstractControl, displayName: string,
    customMessages: { [key: string]: string } | null = null, additionalInfo: any = null): string[] {
    var errors: string[] = [];
    Object.keys(control.errors || {}).forEach((key) => {
      switch (key) {
        case 'required':
          errors.push(`${displayName} ${customMessages?.[key] ?? "is required."}`);
          break;
        case 'pattern':
          errors.push(`${displayName} ${customMessages?.[key] ?? "contains invalid characters."}`);
          break;
        case 'isDupeField':
          errors.push(`${displayName} ${customMessages?.[key] ?? "already exists."}`);
          break;
        case 'notPositiveNumber':
          errors.push(`${displayName} ${customMessages?.[key] ?? "must be a positive number."}`);
          break;
        case 'amountExceededCurrent':
          errors.push(`${displayName} ${customMessages?.[key] ?? "input must not exceed the current amount:"} ${additionalInfo}.`);
          break;
        default:
          errors.push(`${displayName} is invalid.`);
          break;
      }
    });
    return errors;
  }

}