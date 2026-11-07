import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api/api.tokens';
import { Assignee } from '../tasks/task.models';

@Injectable({ providedIn: 'root' })
export class UserDirectoryService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  getUsers(): Observable<Assignee[]> {
    return this.http.get<Assignee[]>(`${this.apiBaseUrl}/users`);
  }
}
