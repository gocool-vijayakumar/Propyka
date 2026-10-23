import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toaster } from './shared/toaster/toaster';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toaster],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('propyka-web');
}
