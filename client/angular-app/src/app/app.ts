import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthStore } from './core/auth/auth.store';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  constructor(authStore: AuthStore) {
    // Restore the session on startup. If the access token is gone (page
    // reload) the auth interceptor silently refreshes using the httpOnly
    // cookie, then retries the /auth/me call.
    void authStore.initialize();
  }
}