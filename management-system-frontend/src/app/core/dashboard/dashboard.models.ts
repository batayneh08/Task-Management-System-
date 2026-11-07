export interface TaskCounts {
  totalTasks: number;
  toDoTasks: number;
  inProgressTasks: number;
  completedTasks: number;
}

export interface TaskSummary {
  id: string;
  title: string;
  description: string | null;
  status: number;
  priority: number;
  dueDate: string;
  assignedTo: { id: string; fullName: string };
}

export interface DashboardResponse {
  counts: TaskCounts;
  recentTasks: TaskSummary[];
  highPriorityTasks: TaskSummary[];
  myAssignedTasks: TaskSummary[];
}
