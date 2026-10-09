import { Routes } from '@angular/router';
import { authGuard, guestGuard, homeGuard, staffGuard } from './core/guards';
import { LoginComponent } from './pages/login.component';
import { RegisterComponent } from './pages/register.component';
import { DashboardComponent } from './pages/dashboard.component';
import { TicketsComponent } from './pages/tickets.component';
import { TicketNewComponent } from './pages/ticket-new.component';
import { TicketDetailComponent } from './pages/ticket-detail.component';
import { KbComponent } from './pages/kb.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', canActivate: [homeGuard], children: [] },
  { path: 'login', canActivate: [guestGuard], component: LoginComponent },
  { path: 'register', canActivate: [guestGuard], component: RegisterComponent },
  { path: 'dashboard', canActivate: [staffGuard], component: DashboardComponent },
  { path: 'tickets', canActivate: [authGuard], component: TicketsComponent },
  { path: 'tickets/new', canActivate: [authGuard], component: TicketNewComponent },
  { path: 'tickets/:id', canActivate: [authGuard], component: TicketDetailComponent },
  { path: 'kb', canActivate: [authGuard], component: KbComponent },
  { path: '**', redirectTo: '' },
];
