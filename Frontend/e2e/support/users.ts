// Seeded by SSSKL.Web/Registrations/E2ESeeder.cs; keep both files in sync.
export const PASSWORD = 'E2e-Passw0rd!';

export const USERS = {
  admin: { userName: 'e2e-admin', fullName: 'Admin E' },
  user: { userName: 'e2e-user', fullName: 'Gebruiker E' },
  user2: { userName: 'e2e-user2', fullName: 'Tweede E' },
  kiosk: { userName: 'e2e-kiosk', fullName: 'Kiosk E' },
  guest: { userName: 'e2e-guest', fullName: 'Gast E' },
} as const;

export type SeededUser = keyof typeof USERS;

export const PRODUCTS = {
  cola: { name: 'E2E Cola', price: 1.5 },
  bier: { name: 'E2E Bier', price: 2 },
  soldOut: { name: 'E2E Uitverkocht', price: 1 },
} as const;

export const SEEDED_EVENT = 'E2E Kampvuuravond';

export const storageStatePath = (role: SeededUser) => `e2e/.auth/${role}.json`;
