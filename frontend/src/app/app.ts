import { Component, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';

import { EventService } from './core/services/event-service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly eventService = inject(EventService);

  // Header event count only; reuses the same EventService.getEvents() EventList already calls.
  protected readonly eventsResource = rxResource({
    stream: () => this.eventService.getEvents(),
  });
}
