export interface UserStat {
  userId: string;
  totalSpent: number;
  totalOrders: number;
  totalItemsBought: number;
  totalTopUp: number;
  quoteCount: number;
  quoteVotesGiven: number;
  quoteVotesReceived: number;
  reactionCount: number;
  currentStreak: number;
  lastActivityDate?: string;
  membershipStartDate?: string;
  maxOrdersPerHour: number;
  minMinutesBetweenOrders: number;
  minMinutesBetweenTopUp: number;
  maxSingleTopUp: number;
  lastOrderDate?: string;
  lastTopUpDate?: string;
  lastStatsRecalculatedAt?: string;
}
