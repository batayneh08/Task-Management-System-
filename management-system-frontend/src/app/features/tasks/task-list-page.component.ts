import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { UserRole } from '../../core/auth/auth.models';
import { AuthSessionStore } from '../../core/auth/auth-session.store';
import { Assignee, CreateTaskRequest, TaskActivity, TaskFilter, TaskItem, TaskPriority, TaskStatus, UpdateTaskRequest } from '../../core/tasks/task.models';
import { TaskApiService } from '../../core/tasks/task-api.service';
import { UserDirectoryService } from '../../core/users/user-directory.service';

@Component({
  selector: 'app-task-list-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './task-list-page.component.html',
  styleUrl: './task-list-page.component.css',
})
export class TaskListPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly taskApi = inject(TaskApiService);
  private readonly userDirectory = inject(UserDirectoryService);
  private readonly auth = inject(AuthService);
  private readonly session = inject(AuthSessionStore);
  private readonly router = inject(Router);

  readonly tasks = signal<TaskItem[]>([]);
  readonly users = signal<Assignee[]>([]);
  readonly activity = signal<TaskActivity[]>([]);
  readonly activityLoading = signal(false);
  readonly activityError = signal('');
  readonly currentUser = this.session.currentUser;
  readonly isAdmin = computed(() => this.currentUser()?.role === UserRole.Admin);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly pageError = signal('');
  readonly userError = signal('');
  readonly formError = signal('');
  readonly isFormOpen = signal(false);
  readonly editingTask = signal<TaskItem | null>(null);
  readonly activityTask = signal<TaskItem | null>(null);
  readonly taskToDelete = signal<TaskItem | null>(null);
  readonly statusOptions = [
    { value: TaskStatus.ToDo, label: 'To Do' },
    { value: TaskStatus.InProgress, label: 'In Progress' },
    { value: TaskStatus.Completed, label: 'Completed' },
  ];
  readonly priorityOptions = [
    { value: TaskPriority.Low, label: 'Low' },
    { value: TaskPriority.Medium, label: 'Medium' },
    { value: TaskPriority.High, label: 'High' },
  ];

  readonly filters = this.formBuilder.nonNullable.group({
    search: [''],
    status: [''],
    priority: [''],
    assignedToId: [''],
  });

  readonly taskForm = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(4000)]],
    status: [TaskStatus.ToDo],
    priority: [TaskPriority.Medium],
    dueDate: ['', Validators.required],
    assignedToId: ['', Validators.required],
    progressSummary: ['', Validators.maxLength(3000)],
  });

  ngOnInit(): void {
    this.loadTasks();
    this.loadUsers();
  }

  loadUsers(): void {
    this.userDirectory.getUsers().subscribe({
      next: (users) => this.users.set(users),
      error: (error: unknown) => this.userError.set(getApiErrorMessage(error)),
    });
  }

  loadTasks(filter: TaskFilter = this.currentFilter()): void {
    this.loading.set(true);
    this.pageError.set('');
    this.taskApi
      .getTasks(filter)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (tasks) => this.tasks.set(tasks),
        error: (error: unknown) => this.pageError.set(getApiErrorMessage(error)),
      });
  }

  applyFilters(): void {
    this.loadTasks(this.currentFilter());
  }

  clearFilters(): void {
    this.filters.reset({ search: '', status: '', priority: '', assignedToId: '' });
    this.loadTasks({});
  }

  openCreateForm(): void {
    if (!this.isAdmin()) return;

    this.editingTask.set(null);
    this.formError.set('');
    const date = new Date();
    date.setDate(date.getDate() + 7);
    this.taskForm.reset({
      title: '',
      description: '',
      status: TaskStatus.ToDo,
      priority: TaskPriority.Medium,
      dueDate: this.localDateInputValue(date),
      assignedToId: this.currentUser()?.id ?? '',
      progressSummary: '',
    });
    this.isFormOpen.set(true);
  }

  openEditForm(task: TaskItem): void {
    if (!this.canEditTask(task)) return;

    this.editingTask.set(task);
    this.formError.set('');
    this.taskForm.reset({
      title: task.title,
      description: task.description ?? '',
      status: task.status,
      priority: task.priority,
      dueDate: this.apiDateInputValue(task.dueDate),
      assignedToId: task.assignedTo.id,
      progressSummary: '',
    });
    this.isFormOpen.set(true);
  }

  closeForm(): void {
    if (this.saving()) return;
    this.isFormOpen.set(false);
    this.editingTask.set(null);
  }

  saveTask(): void {
    this.formError.set('');
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }

    const value = this.taskForm.getRawValue();
    const task = this.editingTask();
    const canEditTaskDetails = this.isAdmin();
    const request: CreateTaskRequest = {
      title: canEditTaskDetails || !task ? value.title.trim() : task.title,
      description: canEditTaskDetails || !task ? value.description.trim() || null : task.description,
      priority: canEditTaskDetails || !task ? value.priority : task.priority,
      dueDate: canEditTaskDetails || !task
        ? new Date(`${value.dueDate}T23:59:59.000Z`).toISOString()
        : task.dueDate,
      assignedToId: canEditTaskDetails || !task ? value.assignedToId : task.assignedTo.id,
    };
    const request$ = task
      ? this.taskApi.updateTask(task.id, {
          ...request,
          status: value.status,
          progressSummary: canEditTaskDetails ? null : value.progressSummary.trim() || null,
        } satisfies UpdateTaskRequest)
      : this.taskApi.createTask(request);

    this.saving.set(true);
    request$
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.isFormOpen.set(false);
          this.editingTask.set(null);
          this.loadTasks();
        },
        error: (error: unknown) => this.formError.set(getApiErrorMessage(error)),
      });
  }

  askDelete(task: TaskItem): void {
    this.taskToDelete.set(task);
  }

  cancelDelete(): void {
    this.taskToDelete.set(null);
  }

  deleteTask(): void {
    const task = this.taskToDelete();
    if (!task) return;
    this.taskApi.deleteTask(task.id).subscribe({
      next: () => {
        this.taskToDelete.set(null);
        this.loadTasks();
      },
      error: (error: unknown) => {
        this.pageError.set(getApiErrorMessage(error));
        this.taskToDelete.set(null);
      },
    });
  }

  openActivity(task: TaskItem): void {
    this.activityTask.set(task);
    this.activity.set([]);
    this.activityLoading.set(true);
    this.activityError.set('');
    this.taskApi
      .getActivity(task.id)
      .pipe(finalize(() => this.activityLoading.set(false)))
      .subscribe({
        next: (activity) => this.activity.set(activity),
        error: (error: unknown) => this.activityError.set(getApiErrorMessage(error)),
      });
  }

  logout(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }

  statusLabel(status: number): string {
    return this.statusOptions.find((option) => option.value === status)?.label ?? 'Unspecified';
  }

  priorityLabel(priority: number): string {
    return this.priorityOptions.find((option) => option.value === priority)?.label ?? 'Unspecified';
  }

  canEditTask(task: TaskItem): boolean {
    return this.isAdmin() || this.currentUser()?.id === task.assignedTo.id;
  }

  activityStatusUpdate(item: TaskActivity): {
    value: string;
    summary: string | null;
  } | null {
    if (item.action !== 'Updated Status' || !item.newValue) return null;

    try {
      const update = JSON.parse(item.newValue) as { Value?: unknown; Summary?: unknown };
      if (typeof update.Value !== 'string') return null;
      return { value: update.Value, summary: typeof update.Summary === 'string' ? update.Summary : null };
    } catch {
      return null;
    }
  }

  activityValueLabel(field: string, value: string | null): string {
    if (!value) return '—';
    if (field === 'Status') return ({ ToDo: 'To Do', InProgress: 'In Progress', Completed: 'Completed' } as Record<string, string>)[value] ?? value;
    if (field === 'Priority') return ({ Low: 'Low', Medium: 'Medium', High: 'High' } as Record<string, string>)[value] ?? value;
    if (field === 'DueDate') return this.formatDate(value);
    return value;
  }

  formatDate(date: string): string {
    return new Intl.DateTimeFormat('en-US', { timeZone: 'UTC', day: 'numeric', month: 'short', year: 'numeric' }).format(
      new Date(date),
    );
  }

  trackUser(_index: number, user: Assignee): string {
    return user.id;
  }

  private currentFilter(): TaskFilter {
    const value = this.filters.getRawValue();
    return {
      search: value.search.trim() || undefined,
      status: value.status === '' ? undefined : Number(value.status),
      priority: value.priority === '' ? undefined : Number(value.priority),
      assignedToId: value.assignedToId || undefined,
    };
  }

  private localDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private apiDateInputValue(value: string): string {
    const date = new Date(value);
    const year = date.getUTCFullYear();
    const month = String(date.getUTCMonth() + 1).padStart(2, '0');
    const day = String(date.getUTCDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
