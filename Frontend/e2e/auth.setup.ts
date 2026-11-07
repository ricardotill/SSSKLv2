import { test as setup } from '@playwright/test';
import { uiLoginAndWait } from './support/login';
import { storageStatePath, type SeededUser } from './support/users';

const roles: SeededUser[] = ['admin', 'user', 'kiosk'];

for (const role of roles) {
  setup(`authenticate as ${role}`, async ({ page }) => {
    await uiLoginAndWait(page, role);
    await page.context().storageState({ path: storageStatePath(role) });
  });
}
