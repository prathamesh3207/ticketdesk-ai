import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.user() ? true : inject(Router).parseUrl('/login');
};

export const staffGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (!auth.user()) return inject(Router).parseUrl('/login');
  return auth.isStaff() ? true : inject(Router).parseUrl('/tickets');
};

/** Sends "/" to the right landing page for the role. */
export const homeGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.user()) return router.parseUrl('/login');
  return router.parseUrl(auth.isStaff() ? '/dashboard' : '/tickets');
};

/** Logged-in users do not need the login/register pages. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.user() ? inject(Router).parseUrl('/') : true;
};
