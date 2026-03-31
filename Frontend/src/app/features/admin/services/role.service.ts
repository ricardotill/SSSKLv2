import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Role, CreateRoleRequest } from '../../../core/models/role.model';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  private http = inject(HttpClient);
  private readonly baseUrl = `/api/v1/roles`;

  getAllRoles(): Observable<Role[]> {
    return this.http.get<Role[]>(this.baseUrl);
  }

  getAdminRoles(): Observable<Role[]> {
    return this.http.get<Role[]>(`${this.baseUrl}/admin`);
  }

  createRole(role: CreateRoleRequest): Observable<Role> {
    return this.http.post<Role>(this.baseUrl, role);
  }

  updateRole(id: string, role: CreateRoleRequest): Observable<Role> {
    return this.http.put<Role>(`${this.baseUrl}/${id}`, role);
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
