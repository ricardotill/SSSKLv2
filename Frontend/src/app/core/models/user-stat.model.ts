export interface UserStat {
  userId: string;
  totalSpent: number;
  totalOrders: number;
  totalItemsBought: number;
  totalTopUp: number;
  quoteCount: number;
  quoteVotesGiven: number;
  reactionCount: number;
  currentStreak: number;
  lastActivityDate?: string;
}
