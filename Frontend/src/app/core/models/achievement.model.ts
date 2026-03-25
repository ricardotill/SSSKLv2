export enum ActionOption {
  None = 'None',
  UserOrderAmountBought = 'UserOrderAmountBought',
  UserOrderAmountPaid = 'UserOrderAmountPaid',
  UserIndividualTopUp = 'UserIndividualTopUp',
  UserTotalTopUp = 'UserTotalTopUp',
  YearsOfMembership = 'YearsOfMembership',
  OrdersWithinHour = 'OrdersWithinHour',
  MinutesBetweenOrders = 'MinutesBetweenOrders',
  MinutesBetweenTopUp = 'MinutesBetweenTopUp'
}

export enum ComparisonOperatorOption {
  None = 'None',
  LessThan = 'LessThan',
  GreaterThan = 'GreaterThan',
  LessThanOrEqual = 'LessThanOrEqual',
  GreaterThanOrEqual = 'GreaterThanOrEqual'
}

export interface Achievement {
  id: string;
  name: string;
  description: string;
  autoAchieve: boolean;
  action: ActionOption;
  comparisonOperator: ComparisonOperatorOption;
  comparisonValue: number;
  image?: AchievementImage;
}

export interface AchievementEntry {
  id: string;
  achievementId: string;
  achievementName: string;
  achievementDescription: string;
  dateAdded: string;
  imageUrl?: string;
  hasSeen: boolean;
  userId: string;
}

export interface AchievementListing {
  name: string;
  description: string;
  dateAdded?: string;
  imageUrl?: string;
  completed: boolean;
}

export interface AchievementImage {
  id: string;
  fileName: string;
  uri: string;
  contentType: string;
}

export interface PaginationObject<T> {
  items: T[];
  totalCount: number;
}

export interface AchievementUpdateDto {
  id: string;
  name: string;
  description: string;
  autoAchieve: boolean;
  action: ActionOption;
  comparisonOperator: ComparisonOperatorOption;
  comparisonValue: number;
  image?: AchievementImage;
}
