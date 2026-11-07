import { expect, test } from '@playwright/test';
import { Api, euro } from './support/api';
import { PRODUCTS, USERS, storageStatePath } from './support/users';

test.describe('POS as a member', () => {
  test.use({ storageState: storageStatePath('user') });

  test('ordering a product charges the member and shows in their orders', async ({ page, request }) => {
    const api = new Api(request);
    const before = await api.saldo('user');

    await page.goto('/pos');
    await expect(page.getByRole('checkbox', { name: USERS.user.fullName })).toBeChecked();
    await page.getByRole('checkbox', { name: `${PRODUCTS.cola.name} ${euro(PRODUCTS.cola.price)}` }).check({ force: true });
    await page.getByRole('button', { name: /^Bestellen/ }).click();

    await expect(page.locator('.p-toast-message-success')).toContainText('Order geplaatst!');
    await expect.poll(() => api.saldo('user')).toBeCloseTo(before - PRODUCTS.cola.price, 2);
    await expect(page.getByRole('banner')).toContainText(euro(before - PRODUCTS.cola.price));

    await page.goto('/orders/personal');
    await expect(page.getByRole('cell', { name: PRODUCTS.cola.name }).first()).toBeVisible();
  });

  test('out-of-stock products are not offered', async ({ page }) => {
    await page.goto('/pos');

    await expect(page.getByRole('checkbox', { name: new RegExp(PRODUCTS.bier.name) })).toBeVisible();
    await expect(page.getByRole('checkbox', { name: new RegExp(PRODUCTS.soldOut.name) })).toHaveCount(0);
  });
});

test.describe('POS on the kiosk', () => {
  test.use({ storageState: storageStatePath('kiosk') });

  test('the kiosk orders for a member and charges that member', async ({ page, request }) => {
    const api = new Api(request);
    const before = await api.saldo('user2');

    await page.goto('/pos');
    await expect(page.getByRole('checkbox', { name: USERS.kiosk.fullName })).toHaveCount(0);
    await page.getByRole('checkbox', { name: `${PRODUCTS.bier.name} ${euro(PRODUCTS.bier.price)}` }).check({ force: true });
    await page.getByRole('checkbox', { name: USERS.user2.fullName }).check({ force: true });
    await page.getByRole('button', { name: /^Bestellen/ }).click();

    await expect(page.locator('.p-toast-message-success')).toContainText('Order geplaatst!');
    await expect.poll(() => api.saldo('user2')).toBeCloseTo(before - PRODUCTS.bier.price, 2);
  });
});
