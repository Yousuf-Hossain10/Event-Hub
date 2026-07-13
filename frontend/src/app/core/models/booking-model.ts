export type BookingStatus = 'Confirmed' | 'Cancelled';

export interface BookingModel {
  id: string;
  eventId: string;
  attendeeId: string;
  status: BookingStatus;
  createdAt: string;
  idempotencyKey: string;
}

export interface CreateBookingRequest {
  eventId: string;
  attendeeId: string;
  idempotencyKey: string;
}
