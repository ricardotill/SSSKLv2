export interface ReactionDto {
  id: string;
  userId: string;
  userName: string;
  profilePictureUrl?: string;
  content: string;
  targetId: string;
  targetType: string;
  targetUserName?: string;
  createdOn: Date;
  reactions?: ReactionDto[];
}

export type ReactionTargetType = 'Event' | 'Announcement' | 'Reaction' | 'Quote';

export interface ToggleReactionRequest {
  targetId: string;
  targetType: ReactionTargetType;
  content: string;
}
