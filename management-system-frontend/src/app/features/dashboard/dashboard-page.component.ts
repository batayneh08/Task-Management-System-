import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { AuthSessionStore } from '../../core/auth/auth-session.store';
import { DashboardService } from '../../core/dashboard/dashboard.service';
import { DashboardResponse, TaskSummary } from '../../core/dashboard/dashboard.models';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.css',
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly auth = inject(AuthService);
  private readonly session = inject(AuthSessionStore);
  private readonly router = inject(Router);

  readonly dashboard = signal<DashboardResponse | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly currentUser = this.session.currentUser;

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.dashboardService
      .getDashboard()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => this.dashboard.set(response),
        error: (error: unknown) => this.errorMessage.set(getApiErrorMessage(error)),
      });
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  statusLabel(status: number): string {
    return ['To Do', 'In Progress', 'Completed'][status] ?? 'Unspecified';
  }

  priorityLabel(priority: number): string {
    return ['Low', 'Medium', 'High'][priority] ?? 'Unspecified';
  }

  dueDate(task: TaskSummary): string {
    return new Intl.DateTimeFormat('en-US', { timeZone: 'UTC', day: 'numeric', month: 'short' }).format(
      new Date(task.dueDate),
    );
  }
}
