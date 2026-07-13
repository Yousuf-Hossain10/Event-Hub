import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ApiErrorResponse } from '../../../core/models/api-error-response';
import { BookingModel } from '../../../core/models/booking-model';
import { BookingService } from '../../../core/services/booking-service';
import { EventService } from '../../../core/services/event-service';
import { VenueService } from '../../../core/services/venue-service';

const GUID_PATTERN = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

@Component({
  selector: 'app-event-detail',
  imports: [ReactiveFormsModule],
  templateUrl: './event-detail.html',
  styleUrl: './event-detail.scss',
})
export class EventDetail {
  // Populated from the :id route param by withComponentInputBinding(); updates reactively on
  // every navigation to this route, including detail-to-detail without an intermediate list visit.
  readonly id = input.required<string>();

  private readonly eventService = inject(EventService);
  private readonly venueService = inject(VenueService);
  private readonly bookingService = inject(BookingService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

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

  // Known simplification: a plain GUID text field standing in for a real attendee picker, which
  // doesn't exist yet (no Attendee UI/CRUD has been built).
  protected readonly bookingForm = this.formBuilder.group({
    attendeeId: ['', [Validators.required, Validators.pattern(GUID_PATTERN)]],
  });

  protected readonly submitting = signal(false);
  protected readonly bookingResult = signal<BookingModel | null>(null);
  protected readonly bookingErrorMessage = signal<string | null>(null);

  protected submitBooking(): void {
    if (this.bookingForm.invalid) {
      this.bookingForm.markAllAsTouched();
      return;
    }

    const eventId = this.eventResource.value()?.id;
    if (!eventId) {
      return;
    }

    this.submitting.set(true);
    this.bookingResult.set(null);
    this.bookingErrorMessage.set(null);

    this.bookingService
      .createBooking({
        eventId,
        attendeeId: this.bookingForm.controls.attendeeId.value,
        idempotencyKey: crypto.randomUUID(),
      })
      .subscribe({
        next: (booking) => {
          this.submitting.set(false);
          this.bookingResult.set(booking);
          this.bookingForm.reset();
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.bookingErrorMessage.set(this.resolveBookingErrorMessage(err));
        },
      });
  }

  // Distinguishes the three failure shapes the booking API returns, keyed on HTTP status plus the
  // structured error code — matching on err.error.message text would be fragile.
  private resolveBookingErrorMessage(err: HttpErrorResponse): string {
    const code = (err.error as ApiErrorResponse | null)?.code;

    if (err.status === 422) {
      return 'That event or attendee could not be found. Double-check the IDs and try again.';
    }
    if (err.status === 409 && code === 'Booking.AlreadyBooked') {
      return 'This attendee already has a booking for this event.';
    }
    if (err.status === 409 && code === 'Booking.CannotAcceptBooking') {
      return 'This event is full or not currently open for booking.';
    }
    return 'Something went wrong while creating the booking. Please try again.';
  }
}
