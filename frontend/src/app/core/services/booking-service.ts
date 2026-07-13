import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BookingModel, CreateBookingRequest } from '../models/booking-model';

@Injectable({
  providedIn: 'root',
})
export class BookingService {
  private readonly http = inject(HttpClient);

  createBooking(request: CreateBookingRequest): Observable<BookingModel> {
    return this.http.post<BookingModel>(`${environment.apiUrl}/api/bookings`, request);
  }
}
