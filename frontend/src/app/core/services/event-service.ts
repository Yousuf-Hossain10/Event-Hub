import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { EventModel } from '../models/event-model';

@Injectable({
  providedIn: 'root',
})
export class EventService {
  private readonly http = inject(HttpClient);

  getEvents(): Observable<EventModel[]> {
    return this.http.get<EventModel[]>(`${environment.apiUrl}/api/events`);
  }
}
