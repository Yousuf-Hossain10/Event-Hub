export type EventStatus = 'Draft' | 'Published' | 'Cancelled' | 'Completed';

export interface EventModel {
  id: string;
  title: string;
  description: string;
  startDate: string;
  capacity: number;
  status: EventStatus;
  venueId: string;
  rowVersion: string;
}
