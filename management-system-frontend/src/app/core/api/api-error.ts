import { HttpErrorResponse } from '@angular/common/http';

export function getApiErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'An unexpected error occurred. Please try again.';
  }

  if (error.status === 0) {
    return 'Could not connect to the server. Please check that the API is running.';
  }

  const body = error.error as { detail?: string; title?: string } | null;
  return body?.detail ?? body?.title ?? `The request failed (HTTP ${error.status}). Check your information and try again.`;
}
