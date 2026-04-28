import { ApplicationUserDto } from './application-user.model';

export interface PaginationObject<T> {
  items: T[];
  totalCount: number;
}

export interface QuoteDto {
  id: string;
  text: string;
  dateSaid: string;
  createdOn: string;
  createdBy: ApplicationUserDto | null;
  authors: QuoteAuthorDto[];
  visibleToRoles: string[];
  voteCount: number;
  commentsCount: number;
  hasVoted: boolean;
}

export interface QuoteAuthorDto {
  id: string;
  applicationUserId: string | null;
  applicationUser: ApplicationUserDto | null;
  customName: string | null;
}

export interface QuoteCreateDto {
  text: string;
  dateSaid: string;
  authors: QuoteAuthorCreateDto[];
  visibleToRoles: string[];
  sendNotification: boolean;
}

export interface QuoteAuthorCreateDto {
  applicationUserId: string | null;
  customName: string | null;
}

export interface QuoteUpdateDto {
  text: string;
  dateSaid: string;
  authors: QuoteAuthorCreateDto[];
  visibleToRoles: string[];
}
