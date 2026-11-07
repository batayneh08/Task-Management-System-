import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api.tokens';
import { CreateTaskRequest, TaskActivity, TaskFilter, TaskItem, UpdateTaskRequest } from './task.models';

@Injectable({ providedIn: 'root' })
export class TaskApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  getTasks(filter: TaskFilter = {}): Observable<TaskItem[]> {
    let params = new HttpParams();
    if (filter.status !== undefined) params = params.set('status', filter.status);
    if (filter.priority !== undefined) params = params.set('priority', filter.priority);
    if (filter.assignedToId) params = params.set('assignedToId', filter.assignedToId);
    if (filter.search) params = params.set('search', filter.search);
    return this.http.get<TaskItem[]>(`${this.apiBaseUrl}/tasks`, { params });
  }

  createTask(request: CreateTaskRequest): Observable<TaskItem> {
    return this.http.post<TaskItem>(`${this.apiBaseUrl}/tasks`, request);
  }

  updateTask(id: string, request: UpdateTaskRequest): Observable<TaskItem> {
    return this.http.put<TaskItem>(`${this.apiBaseUrl}/tasks/${id}`, request);
  }

  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/tasks/${id}`);
  }

  getActivity(id: string): Observable<TaskActivity[]> {
    return this.http.get<TaskActivity[]>(`${this.apiBaseUrl}/tasks/${id}/activity`);
  }

}
