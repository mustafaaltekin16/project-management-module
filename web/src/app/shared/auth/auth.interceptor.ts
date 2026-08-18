import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

interface ApiErrorBody {
  error?: string | null;
}

function toUserFacingError(error: unknown): unknown {
  if (!(error instanceof HttpErrorResponse)) {
    return error;
  }

  const body = error.error as ApiErrorBody | null;
  if (body?.error) {
    return new Error(body.error);
  }

  if (error.status === 0) {
    return new Error('Sunucuya bağlanılamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.');
  }
  if (error.status === 401) {
    return new Error('Oturumunuz sona ermiş. Lütfen tekrar giriş yapın.');
  }
  if (error.status === 403) {
    return new Error('Bu işlemi yapmaya yetkiniz yok.');
  }
  if (error.status >= 500) {
    return new Error('Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.');
  }
  return new Error('İstek tamamlanamadı.');
}

/** Attaches the stored session token to every request aimed at the backend gateway; forces re-login on 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }

  const isLoginRequest = req.url.includes('/api/auth/login');
  const auth = inject(AuthService);
  const token = !isLoginRequest ? auth.getToken() : null;
  const authedReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authedReq).pipe(
    catchError((error: unknown) => {
      // A failed login attempt (wrong credentials) is not a session expiring — don't force a logout/redirect loop for it.
      if (!isLoginRequest && error instanceof HttpErrorResponse && error.status === 401) {
        auth.logout();
      }
      return throwError(() => toUserFacingError(error));
    })
  );
};
