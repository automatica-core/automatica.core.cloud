import { HttpBackend, HttpClient, HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Observable, catchError, firstValueFrom, from, switchMap, throwError } from 'rxjs';
import { AuthResponse, User, roleAtLeast } from './api';

const authBase = '/webapi/v1/auth';
// the api rejects refresh/logout without this header (CSRF guard for the cookie)
export const csrfHeader = { 'X-Requested-With': 'automatica-cloud' };

/**
 * Access token lives in memory only (15 min). The refresh token is an HttpOnly, SameSite=Strict cookie scoped
 * to /webapi/v1/auth, so scripts never see it; a reload silently refreshes.
 */
@Injectable({ providedIn: 'root' })
export class Auth {
  // raw client without the interceptor, so refresh never recurses
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly router = inject(Router);

  private accessToken: string | null = null;
  private expiresAt = 0;
  private refreshing: Promise<boolean> | null = null;
  private timer: ReturnType<typeof setTimeout> | null = null;

  readonly user = signal<User | null>(null);
  readonly isAdmin = computed(() => roleAtLeast(this.user()?.role, 'Administrator'));
  readonly isSysAdmin = computed(() => this.user()?.role === 'SystemAdministrator');

  get token(): string | null {
    return this.accessToken && Date.now() < this.expiresAt ? this.accessToken : null;
  }

  async login(userName: string, password: string): Promise<void> {
    const res = await firstValueFrom(this.http.post<AuthResponse>(`${authBase}/login`, { userName, password }));
    this.apply(res);
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${authBase}/password`, { currentPassword, newPassword }, { headers: this.bearer() }),
    );
    this.apply(res);
  }

  /** Tries the refresh cookie once; concurrent callers share the same request. */
  refresh(): Promise<boolean> {
    this.refreshing ??= firstValueFrom(this.http.post<AuthResponse>(`${authBase}/refresh`, null, { headers: csrfHeader }))
      .then((res) => {
        this.apply(res);
        return true;
      })
      .catch(() => {
        this.clear();
        return false;
      })
      .finally(() => (this.refreshing = null));
    return this.refreshing;
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post(`${authBase}/logout`, null, { headers: csrfHeader }));
    } catch {
      // the cookie is gone either way once the session ends
    }
    this.clear();
    await this.router.navigateByUrl('/login');
  }

  private bearer(): Record<string, string> {
    return this.accessToken ? { Authorization: `Bearer ${this.accessToken}` } : {};
  }

  private apply(res: AuthResponse): void {
    this.accessToken = res.accessToken;
    this.expiresAt = new Date(res.expiresAt).getTime();
    this.user.set(res.user);
    if (this.timer) {
      clearTimeout(this.timer);
    }
    // refresh a minute before expiry
    const due = Math.max(10_000, this.expiresAt - Date.now() - 60_000);
    this.timer = setTimeout(() => void this.refresh(), due);
  }

  private clear(): void {
    this.accessToken = null;
    this.expiresAt = 0;
    this.user.set(null);
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/webapi/')) {
    return next(req);
  }
  const auth = inject(Auth);
  const withToken = (r: HttpRequest<unknown>, token: string | null) =>
    token ? r.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : r;

  return next(withToken(req, auth.token)).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }
      // one silent refresh, then retry; otherwise back to login
      return from(auth.refresh()).pipe(
        switchMap((ok): Observable<never> | ReturnType<typeof next> => {
          if (!ok) {
            void auth.logout();
            return throwError(() => err);
          }
          return next(withToken(req, auth.token));
        }),
      );
    }),
  );
};

const ensureSession = async (): Promise<Auth> => {
  const auth = inject(Auth);
  if (!auth.user() || !auth.token) {
    await auth.refresh();
  }
  return auth;
};

export const authGuard: CanActivateFn = async (_route, state) => {
  const router = inject(Router);
  const auth = await ensureSession();
  return auth.user() ? true : router.createUrlTree(['/login'], { queryParams: { next: state.url } });
};

export const adminGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const auth = await ensureSession();
  return auth.isAdmin() ? true : router.createUrlTree(['/servers']);
};

export const sysAdminGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const auth = await ensureSession();
  return auth.isSysAdmin() ? true : router.createUrlTree(['/servers']);
};
