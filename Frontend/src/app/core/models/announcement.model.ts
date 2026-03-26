export interface Announcement {
  message: string;
  description?: string;
  fotoUrl?: string;
  url?: string;
  order: number;
  isScheduled: boolean;
  plannedFrom?: string;
  plannedTill?: string;
  id: string;
  createdOn: string;
}

export interface AnnouncementCreateDto {
  message: string;
  description?: string;
  order: number;
  isScheduled: boolean;
  plannedFrom?: string;
  plannedTill?: string;
}

export interface AnnouncementUpdateDto {
  message: string;
  description?: string;
  order: number;
  isScheduled: boolean;
  plannedFrom?: string;
  plannedTill?: string;
}

export interface PaginatedAnnouncements {
  items: Announcement[];
  totalCount: number;
}
