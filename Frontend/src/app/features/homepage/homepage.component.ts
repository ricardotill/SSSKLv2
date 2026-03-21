import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-homepage',
  imports: [CardModule],
  template: `
    <div class="dashboard-grid">
      <p-card header="Total Users" styleClass="stat-card">
        <p class="stat-value">{{ totalUsers() }}</p>
      </p-card>
      <p-card header="Revenue" styleClass="stat-card">
        <p class="stat-value">{{ revenue() }}</p>
      </p-card>
      <p-card header="Active Sessions" styleClass="stat-card">
        <p class="stat-value">{{ activeSessions() }}</p>
      </p-card>
    </div>
  `,
  styles: `
    .dashboard-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
    }
    :host ::ng-deep .stat-card .p-card-header,
    :host ::ng-deep .stat-card .p-card-title {
      font-size: 1rem;
      color: var(--p-text-muted-color, #64748b);
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin-bottom: 0;
    }
    .stat-value {
      margin: 0;
      font-size: 2rem;
      font-weight: bold;
      color: var(--p-text-color, #0f172a);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class HomepageComponent {
  totalUsers = signal(1254);
  revenue = signal('$12,450');
  activeSessions = signal(342);
}
