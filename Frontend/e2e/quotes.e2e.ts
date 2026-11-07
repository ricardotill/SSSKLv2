import { expect, test } from '@playwright/test';
import { USERS, storageStatePath } from './support/users';

test.use({ storageState: storageStatePath('user') });

test('a member adds a quote and votes on it', async ({ page }) => {
  const text = `Wie het laatst lacht ${Date.now()}`;

  await page.goto('/quotes');
  await page.getByRole('button', { name: 'Quote Toevoegen' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.locator('#text').fill(text);
  await dialog.getByRole('combobox', { name: /Selecteer een gebruiker/ }).fill(USERS.user2.fullName);
  await page.getByRole('option', { name: USERS.user2.fullName }).click();
  await dialog.getByRole('button', { name: 'Opslaan' }).click();

  await expect(dialog).toBeHidden();
  const card = page.locator('app-quote-card').filter({ hasText: text });
  await expect(card).toBeVisible();

  // The vote control is the heart icon with the vote count next to it.
  const votes = card.locator('div:has(> i.pi-heart), div:has(> i.pi-heart-fill)');
  await expect(votes).toHaveText('0');
  await votes.click();
  await expect(votes).toHaveText('1');
  await expect(votes.locator('i.pi-heart-fill')).toBeVisible();
});
