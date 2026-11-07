import { expect, type Page } from '@playwright/test';
import { PASSWORD, USERS, type SeededUser } from './users';

/**
 * Logs in through the login page. "Onthoud mij" is checked so the tokens land in localStorage,
 * which Playwright's storageState captures (sessionStorage is not captured).
 */
export async function uiLogin(page: Page, role: SeededUser, password = PASSWORD, { navigate = true } = {}): Promise<void> {
  // Pass navigate: false when already on the login page, e.g. after a guard redirect with a returnUrl.
  if (navigate) await page.goto('/login');
  await page.locator('#userName').fill(USERS[role].userName);
  await page.locator('#password input').fill(password);
  await page.locator('label[for="rememberMe"]').click();
  await page.getByRole('button', { name: 'Inloggen' }).click();
}

export async function uiLoginAndWait(page: Page, role: SeededUser): Promise<void> {
  await uiLogin(page, role);
  await expect(page).toHaveURL(/\/pos$/);
}
