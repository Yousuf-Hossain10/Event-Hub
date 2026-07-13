import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { VenueModel } from '../models/venue-model';

@Injectable({
  providedIn: 'root',
})
export class VenueService {
  private readonly http = inject(HttpClient);

  getVenueById(id: string): Observable<VenueModel> {
    return this.http.get<VenueModel>(`${environment.apiUrl}/api/venues/${id}`);
  }
}
