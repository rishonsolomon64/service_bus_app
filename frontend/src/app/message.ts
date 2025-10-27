import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Details } from '../app/details'; 

// This interface matches our new C# ErrorLog model.
export interface ErrorLog {
    message: string;
    timestamp?: string;
}

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  private apiUrl = 'https://localhost:7268/Message';

  constructor(private http: HttpClient) { }

  publish(details: Details): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/publish`, details);
  }

  // Add this method for scheduling
  schedule(payload: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/schedule`, payload);
  }

  // This is the new method to send the error log.
  logError(errorMessage: string): Observable<any> {
      const errorLog: ErrorLog = {
          message: errorMessage
      };
      return this.http.post<any>(`${this.apiUrl}/logError`, errorLog);
  }
}