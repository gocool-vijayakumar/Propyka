import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  createdAt: string;
  isDeleted: boolean;
  deletedAt: string | null;
  isLockedOut: boolean;
  roles: string[];
}

export interface PagedResult<T> {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  items: T[];
}

export type UserStatusFilter = 'active' | 'deleted' | 'all';

export interface UserQuery {
  search?: string;
  status?: UserStatusFilter;
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class Admin {

  private http = inject(HttpClient);

  private readonly apiUrl = environment.apiUrl;

  getUsers(query: UserQuery): Observable<PagedResult<AdminUser>> {
    let params = new HttpParams()
      .set('status', query.status ?? 'active')
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 20);

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    return this.http.get<PagedResult<AdminUser>>(
      `${this.apiUrl}/api/admin/users`,
      { params }
    );
  }

  trashUser(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/api/admin/users/${id}/trash`,
      {}
    );
  }

  restoreUser(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/api/admin/users/${id}/restore`,
      {}
    );
  }

  setRoles(id: string, roles: string[]): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/admin/users/${id}/roles`,
      { roles }
    );
  }
}