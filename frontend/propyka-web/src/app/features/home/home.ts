import { Component, inject, OnInit, signal } from '@angular/core';
import { User } from '../../core/services/user';
import { Auth } from '../../core/services/auth';
import { Router } from '@angular/router';



@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {

  private userService = inject(User);
  private auth = inject(Auth);


  message = signal('Loading...');
  userId = signal('');

private router = inject(Router);

logout(): void {
  this.auth.logout();
  this.router.navigate(['/login']);
}

  ngOnInit(): void {
    console.log('Home initialized');

    this.userService.getCurrentUser().subscribe({
      next: (response) => {
        console.log('API SUCCESS:', response);

        this.message.set(response.message);
        this.userId.set(response.userId);
      },
      error: (error) => {
        console.error('API ERROR:', error);

        this.message.set('Unable to load authenticated user.');
      }
    });
  }
}
