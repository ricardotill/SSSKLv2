import { expect, type APIRequestContext } from '@playwright/test';
import { PASSWORD, USERS, type SeededUser } from './users';

/** Direct API access (through the dev server proxy) for arranging and checking state. */
export class Api {
  private readonly tokens = new Map<SeededUser, string>();

  constructor(private readonly request: APIRequestContext) {}

  async token(role: SeededUser): Promise<string> {
    const cached = this.tokens.get(role);
    if (cached) return cached;

    const response = await this.request.post('/api/v1/identity/login', {
      data: { userName: USERS[role].userName, password: PASSWORD },
    });
    expect(response.status(), await response.text()).toBe(200);
    const { accessToken } = (await response.json()) as { accessToken: string };
    this.tokens.set(role, accessToken);
    return accessToken;
  }

  async get<T>(role: SeededUser, url: string): Promise<T> {
    const response = await this.request.get(url, { headers: { Authorization: `Bearer ${await this.token(role)}` } });
    expect(response.status(), await response.text()).toBe(200);
    return (await response.json()) as T;
  }

  async saldo(role: SeededUser): Promise<number> {
    const me = await this.get<{ saldo: number }>(role, '/api/v1/applicationuser/me');
    return me.saldo;
  }
}

/** Formats an amount like the app does, e.g. 1.5 -> "€ 1,50" (with a non-breaking space). */
export const euro = (amount: number) =>
  new Intl.NumberFormat('nl-NL', { style: 'currency', currency: 'EUR' }).format(amount);
