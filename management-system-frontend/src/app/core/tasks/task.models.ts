export enum TaskStatus {
  ToDo = 0,
  InProgress = 1,
  Completed = 2,
}

export enum TaskPriority {
  Low = 0,
  Medium = 1,
  High = 2,
}

export interface TaskUser {
  id: string;
  fullName: string;
}

export interface TaskItem {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate: string;
  createdBy: TaskUser;
  assignedTo: TaskUser;
  createdAt: string;
  updatedAt: string;
}

export interface TaskFilter {
  status?: number;
  priority?: number;
  assignedToId?: string;
  search?: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueDate: string;
  assignedToId: string;
}

export interface UpdateTaskRequest extends CreateTaskRequest {
  status: TaskStatus;
  progressSummary?: string | null;
}

export interface TaskActivity {
  id: string;
  taskId: string | null;
  taskTitle: string;
  userId: string;
  userFullName: string;
  action: string;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
}

export interface Assignee {
  id: string;
  fullName: string;
  email: string;
  role: number;
  createdAt: string;
}
