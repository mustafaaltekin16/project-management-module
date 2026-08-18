import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../api/api-response';
import { unwrap } from '../api/unwrap';

export interface CurrentUser {
  userId: string;
  displayName: string;
  roles: string[];
}

interface JwtPayload {
  sub: string;
  name: string;
  role?: string | string[];
  exp: number;
}

interface LoginResponse {
  accessToken: string;
}

const TOKEN_STORAGE_KEY = 'cwa-pm-token';

/**
 * This product is managed standalone — there is no corporate SSO/OIDC provider to hand off to — so it
 * owns real sign-in end-to-end: a login screen, a token issued only after the backend verifies email +
 * password against the employee directory (see UserDirectoryService/AuthController), and a stored
 * session that survives a page reload until the token itself expires.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private token: string | null = null;
  readonly currentUser = signal<CurrentUser | null>(null);
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  constructor() {
    this.restoreSession();
  }

  async login(email: string, password: string): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiResponse<LoginResponse>>(`${environment.apiBaseUrl}/api/auth/login`, { email, password })
    );
    const accessToken = unwrap(response).accessToken;
    const payload = this.decodeToken(accessToken);
    if (!payload) {
      throw new Error('Sunucudan geçersiz bir oturum bilgisi alındı.');
    }

    this.token = accessToken;
    this.currentUser.set(this.toCurrentUser(payload));
    localStorage.setItem(TOKEN_STORAGE_KEY, accessToken);
  }

  logout(): void {
    this.token = null;
    this.currentUser.set(null);
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this.token;
  }

  hasAnyRole(roles: string[]): boolean {
    const current = this.currentUser()?.roles ?? [];
    return roles.some((role) => current.includes(role));
  }

  private restoreSession(): void {
    const stored = localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!stored) {
      return;
    }

    const payload = this.decodeToken(stored);
    if (!payload || payload.exp * 1000 <= Date.now()) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      return;
    }

    this.token = stored;
    this.currentUser.set(this.toCurrentUser(payload));
  }

  private toCurrentUser(payload: JwtPayload): CurrentUser {
    const roles = Array.isArray(payload.role) ? payload.role : payload.role ? [payload.role] : [];
    return { userId: payload.sub, displayName: payload.name, roles };
  }

  private decodeToken(token: string): JwtPayload | null {
    try {
      const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      // atob() gives one JS char per byte, not per UTF-8 code point — for non-ASCII claims (Turkish
      // names in "name"/"role") that mangles multi-byte characters unless re-decoded as UTF-8.
      const bytes = Uint8Array.from(atob(base64), (char) => char.charCodeAt(0));
      const json = new TextDecoder('utf-8').decode(bytes);
      return JSON.parse(json) as JwtPayload;
    } catch {
      return null;
    }
  }
}
