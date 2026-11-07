import { computed, Injectable, signal } from '@angular/core';
import { AuthResponse, AuthenticatedUser } from './auth.models';

const SESSION_KEY = 'task-management-session';

@Injectable({ providedIn: 'root' })
export class AuthSessionStore {
  private readonly session = signal<AuthResponse | null>(this.readSession());

  readonly currentUser = computed<AuthenticatedUser | null>(
    () => this.session()?.user ?? null,
  );
  readonly token = computed(() => this.session()?.accessToken ?? null);
  readonly isAuthenticated = computed(() => this.token() !== null);

  save(session: AuthResponse): void {
    window.sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  clear(): void {
    window.sessionStorage.removeItem(SESSION_KEY);
    this.session.set(null);
  }

  private readSession(): AuthResponse | null {
    try {
      const saved = window.sessionStorage.getItem(SESSION_KEY);
      if (!saved) return null;

      const session = JSON.parse(saved) as AuthResponse;
      const expiration = Date.parse(session.expiresAt);
      if (!session.accessToken || !session.user || !Number.isFinite(expiration) || expiration <= Date.now()) {
        window.sessionStorage.removeItem(SESSION_KEY);
        return null;
      }

      return session;
    } catch {
      return null;
    }
  }
}
