import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-admin-orders',
  imports: [],
  template: `
    <div class="bg-surface-0 dark:bg-surface-900 p-8 rounded-xl shadow-md text-surface-900 dark:text-surface-0">
      <h2 class="text-2xl font-bold mb-4">Admin Orders</h2>
      <p class="text-surface-600 dark:text-surface-300">Manage all system orders here.</p>
    </div>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class OrdersComponent { }
