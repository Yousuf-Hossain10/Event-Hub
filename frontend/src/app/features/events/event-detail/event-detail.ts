import { Component, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';

import { EventService } from '../../../core/services/event-service';
import { VenueService } from '../../../core/services/venue-service';

@Component({
  selector: 'app-event-detail',
  imports: [],
  templateUrl: './event-detail.html',
  styleUrl: './event-detail.scss',
})
export class EventDetail {
  // Populated from the :id route param by withComponentInputBinding(); updates reactively on
  // every navigation to this route, including detail-to-detail without an intermediate list visit.
  readonly id = input.required<string>();

  private readonly eventService = inject(EventService);
  private readonly venueService = inject(VenueService);

  protected readonly eventResource = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.eventService.getEventById(id),
  });

  // Chained off the event resource's resolved value: stays idle (no request) until the event
  // has loaded, then re-fetches automatically whenever venueId changes.
  protected readonly venueResource = rxResource({
    params: () => this.eventResource.value()?.venueId,
    stream: ({ params: venueId }) => this.venueService.getVenueById(venueId),
  });
}
