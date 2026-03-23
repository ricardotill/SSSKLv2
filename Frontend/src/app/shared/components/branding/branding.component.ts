import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-branding',
  imports: [TagModule],
  template: `
    <div class="flex items-center">
      <h2 class="text-xl font-bold m-0">SSSKL</h2>
      <p-tag class="ml-2" value="v3.0.0" />
    </div>
  `,
  styles: `
    :host {
      display: block;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BrandingComponent { }
