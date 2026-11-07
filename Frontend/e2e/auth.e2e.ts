import { expect, test } from '@playwright/test';
import { uiLogin, uiLoginAndWait } from './support/login';
import { storageStatePath } from './support/users';

test.describe('login', () => {
  test('valid credentials open the POS', async ({ page }) => {
    await uiLoginAndWait(page, 'user');

    await expect(page.getByRole('heading', { name: 'Wat' })).toBeVisible();
    expect(await page.evaluate(() => localStorage.getItem('access_token'))).toBeTruthy();
  });

  test('wrong password shows an error and stays on the login page', async ({ page }) => {
    await uiLogin(page, 'user', 'Wrong-Passw0rd!');

    await expect(page.locator('p-message')).toBeVisible();
    await expect(page).toHaveURL(/\/login/);
  });

  test('a protected route redirects to login and back after signing in', async ({ page }) => {
    await page.goto('/orders/personal');
    await expect(page).toHaveURL(/\/login\?returnUrl=%2Forders%2Fpersonal/);

    await uiLogin(page, 'user', undefined, { navigate: false });

    await expect(page).toHaveURL(/\/orders\/personal$/);
    await expect(page.getByRole('heading', { name: 'Mijn Bestellingen' })).toBeVisible();
  });
});

test.describe('signed in', () => {
  test.use({ storageState: storageStatePath('user') });

  test('visiting the login page redirects to the POS', async ({ page }) => {
    await page.goto('/login');

    await expect(page).toHaveURL(/\/pos$/);
  });

  test('logging out clears the session', async ({ page }) => {
    await page.goto('/pos');
    await page.getByRole('complementary').getByText('Uitloggen').click();

    await expect(page).not.toHaveURL(/\/pos$/);
    expect(await page.evaluate(() => localStorage.getItem('access_token'))).toBeNull();
    await page.goto('/orders/personal');
    await expect(page).toHaveURL(/\/login/);
  });
});
