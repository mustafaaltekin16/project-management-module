import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../../shared/auth/auth.service';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            login: vi.fn(async () => undefined)
          }
        }
      ]
    }).compileComponents();
  });

  it('uses one account form for every authorized role', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('E-posta veya kullanıcı adı');
    expect(fixture.nativeElement.textContent).not.toContain('Admin Girişi');
    expect(fixture.nativeElement.textContent).not.toContain('Kullanıcı Girişi');
  });

  it('requires both account and password before authentication', async () => {
    const component = TestBed.createComponent(LoginPage).componentInstance;
    const authService = TestBed.inject(AuthService);

    await component.submit();

    expect(component.errorMessage()).toBe('E-posta ve şifre zorunludur.');
    expect(authService.login).not.toHaveBeenCalled();
  });
});
