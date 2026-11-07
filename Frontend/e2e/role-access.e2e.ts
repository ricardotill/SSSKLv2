import { expect, test } from '@playwright/test';
import { storageStatePath } from './support/users';

test.describe('member', () => {
  test.use({ storageState: storageStatePath('user') });

  test('cannot open admin pages and sees no admin menu', async ({ page }) => {
    await page.goto('/admin/products');

    await expect(page).toHaveURL(/\/pos$/);
    await expect(page.getByRole('link', { name: /Producten/ })).toHaveCount(0);
  });
});

test.describe('kiosk', () => {
  test.use({ storageState: storageStatePath('kiosk') });

  test('can open the order overview but not product management', async ({ page }) => {
    await page.goto('/admin/orders');
    await expect(page).toHaveURL(/\/admin\/orders$/);

    await page.goto('/admin/products');
    await expect(page).toHaveURL(/\/pos$/);
  });
});

test.describe('admin', () => {
  test.use({ storageState: storageStatePath('admin') });

  test('can open product management', async ({ page }) => {
    await page.goto('/admin/products');

    await expect(page).toHaveURL(/\/admin\/products$/);
    await expect(page.getByRole('heading', { name: 'Producten' })).toBeVisible();
  });
});
