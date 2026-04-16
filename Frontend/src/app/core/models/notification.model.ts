export interface NotificationDto {
  id: string;
  userId: string;
  title: string;
  message: string;
  isRead: boolean;
  linkUri?: string;
  createdOn: string;
}
