import { Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/services/auth';

/**
 * A placeholder ad. Renders nothing for Pro members and admins. Everyone else
 * sees it. Swap the house copy for a real ad network tag when one is chosen.
 */
@Component({
  selector: 'app-ad-slot',
  imports: [RouterLink],
  templateUrl: './ad-slot.html',
  styleUrl: './ad-slot.css'
})
export class AdSlot {

  readonly auth = inject(Auth);

  /** card sits inside a listing grid. banner spans the full width. */
  readonly variant = input<'card' | 'banner'>('card');
}
