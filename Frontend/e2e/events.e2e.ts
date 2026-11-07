import { expect, type Page, test } from '@playwright/test';
import { SEEDED_EVENT, USERS, storageStatePath } from './support/users';

test.use({ storageState: storageStatePath('user') });

/** Picks a day in next month from a PrimeNG datepicker (typing a date is not parsed reliably). */
async function pickDayNextMonth(page: Page, pickerId: string, day: number) {
  await page.locator(`#${pickerId} input`).click();
  const calendar = page.getByRole('dialog', { name: 'Choose Date' });
  await calendar.getByRole('button', { name: 'Next Month' }).click();
  await calendar.getByRole('gridcell', { name: new RegExp(`, \\w+ ${day}, \\d{4}$`) }).filter({ hasNot: page.locator('[aria-disabled="true"]') }).first().click();
  await page.keyboard.press('Escape');
  await expect(calendar).toBeHidden();
}

test('a member creates an event', async ({ page }) => {
  const title = `E2E Opkomst ${Date.now()}`;

  await page.goto('/events/new');
  await page.locator('#title').fill(title);
  await pickDayNextMonth(page, 'startDateTime', 10);
  await pickDayNextMonth(page, 'endDateTime', 11);
  await page.locator('.ql-editor').click();
  await page.keyboard.type('Gezellige opkomst');
  await page.getByRole('button', { name: 'Opslaan' }).click();

  // Saving returns to the event list, where the new event is shown.
  await expect(page).toHaveURL(/\/events$/);
  await page.getByRole('heading', { name: title }).click();
  await expect(page).toHaveURL(/\/events\/[0-9a-f-]{36}$/);
  await expect(page.getByRole('heading', { name: title })).toBeVisible();
});

test('a member accepts the seeded event and is listed as attending', async ({ page }) => {
  await page.goto('/events');
  await page.getByRole('heading', { name: SEEDED_EVENT }).click();
  await expect(page).toHaveURL(/\/events\/[0-9a-f-]{36}$/);

  await page.getByRole('button', { name: 'Aanwezig' }).click();

  await expect(page.getByText(USERS.user.fullName).first()).toBeVisible();
});
