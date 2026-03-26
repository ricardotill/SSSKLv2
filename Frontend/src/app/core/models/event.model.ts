export enum EventResponseStatus {
  Accepted = 'Accepted',
  Declined = 'Declined'
}

export interface EventResponseUserDto {
  userId: string;
  userName: string;
}

export interface EventCreateDto {
  title: string;
  description: string;
  startDateTime: string;
  endDateTime: string;
}

export interface EventDto {
  id: string;
  title: string;
  description: string;
  imageUrl?: string | null;
  startDateTime: string;
  endDateTime: string;
  creatorName: string;
  createdOn: string;
  acceptedUsers: EventResponseUserDto[];
  declinedUsers: EventResponseUserDto[];
  userResponse?: EventResponseStatus | null;
}

export interface EventResponseDto {
  status: EventResponseStatus;
}

export interface PaginationObject<T> {
  items: T[];
  totalCount: number;
}
