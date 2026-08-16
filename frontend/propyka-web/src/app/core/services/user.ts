import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CurrentUserResponse {
  message: string;
  userId: string;
}

@Injectable({
  providedIn: 'root'
})
export class User {
  private http = inject(HttpClient);

  private readonly apiUrl = environment.apiUrl;

  getCurrentUser(): Observable<CurrentUserResponse> {
    return this.http.get<CurrentUserResponse>(
      `${this.apiUrl}/api/Test/private`
    );
  }
}
