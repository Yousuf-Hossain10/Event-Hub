import { Routes } from '@angular/router';

import { EventDetail } from './features/events/event-detail/event-detail';
import { EventList } from './features/events/event-list/event-list';

export const routes: Routes = [
  { path: '', component: EventList },
  { path: 'events/:id', component: EventDetail },
];
