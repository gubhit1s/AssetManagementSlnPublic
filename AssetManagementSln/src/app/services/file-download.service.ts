import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';


@Injectable({
  providedIn: 'root'
})
export class FileDownloadService {

  constructor(private http: HttpClient, private authService: AuthService,
    private snackBar: MatSnackBar
  ) { 
  }


  /**
   * download file with report progress and save dialog.
   * 
   * @param url 
   * @param fileName 
   * 
   * @return Observable contains progress by percent
   * 
   */
  downloadFile(route: string, filename: string | null = null): void {
    this.snackBar.open("Generating File, please wait...", "Dismiss");
    const token = this.authService.getToken();
    const headers = new HttpHeaders().set('authorization', 'Bearer ' + token);
      this.http.get(route, { headers, responseType: 'blob' as 'json' }).subscribe({
        next: (response: any) => {
          let dataType = response.type;
          let binaryData = [];
          binaryData.push(response);
          let downloadLink = document.createElement('a');
          downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: dataType }));
          if (filename)
            downloadLink.setAttribute('download', filename);
          document.body.appendChild(downloadLink);
          downloadLink.click();
          this.snackBar.dismiss();
        }
      }
    )
  }
}
