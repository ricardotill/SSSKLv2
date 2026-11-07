import { expect, test } from '@playwright/test';
import { Api } from './support/api';
import { USERS, storageStatePath } from './support/users';

test.use({ storageState: storageStatePath('admin') });

test('an admin creates a product that members can order', async ({ page }) => {
  const name = `E2E Product ${Date.now()}`;

  await page.goto('/admin/products');
  await page.getByRole('button', { name: 'Product Toevoegen' }).click();
  const dialog = page.getByRole('dialog', { name: 'Product Toevoegen' });
  await dialog.locator('#name').fill(name);
  await dialog.locator('#price input').fill('2,25');
  await dialog.locator('#stock input').fill('15');
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  await expect(dialog).toBeHidden();
  await expect(page.getByRole('row', { name: new RegExp(name) })).toBeVisible();

  await page.goto('/pos');
  await expect(page.getByRole('checkbox', { name: new RegExp(name) })).toBeVisible();
});

test('an admin tops up a member', async ({ page, request }) => {
  const api = new Api(request);
  const before = await api.saldo('user2');

  await page.goto('/admin/topups');
  await page.getByRole('button', { name: 'Opwaardering Toevoegen' }).click();
  const dialog = page.getByRole('dialog', { name: 'Opwaardering Toevoegen' });
  await dialog.locator('#userName input').fill(USERS.user2.fullName);
  await page.getByRole('option', { name: USERS.user2.fullName }).click();
  await dialog.locator('#saldo input').fill('5');
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  await expect(dialog).toBeHidden();
  await expect.poll(() => api.saldo('user2')).toBeCloseTo(before + 5, 2);
  await expect(page.getByRole('row', { name: new RegExp(USERS.user2.userName) }).first()).toBeVisible();
});
