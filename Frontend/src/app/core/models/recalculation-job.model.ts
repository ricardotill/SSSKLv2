export type RecalculationJobStatus = 'Pending' | 'Running' | 'Completed' | 'Failed';

export interface RecalculationJob {
  id: string;
  status: RecalculationJobStatus;
  totalUsers: number;
  processedUsers: number;
  failedUsers: number;
  startedAt: string;
  completedAt?: string;
  errorMessage?: string;
  startedByUserId: string;
}
