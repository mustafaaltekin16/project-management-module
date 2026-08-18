import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Icon } from '../../../shared/icon/icon';
import { AuthService } from '../../../shared/auth/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [FormsModule, Icon],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly email = signal('');
  readonly password = signal('');
  readonly passwordVisible = signal(false);
  readonly submitting = signal(false);
  readonly errorMessage = signal('');

  async submit(): Promise<void> {
    if (this.submitting()) {
      return;
    }

    const email = this.email().trim();
    const password = this.password();
    if (!email || !password) {
      this.errorMessage.set('E-posta ve şifre zorunludur.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');
    try {
      await this.authService.login(email, password);
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/projects';
      this.router.navigateByUrl(returnUrl);
    } catch (error) {
      this.errorMessage.set(error instanceof Error ? error.message : 'Giriş yapılamadı.');
    } finally {
      this.submitting.set(false);
    }
  }
}
