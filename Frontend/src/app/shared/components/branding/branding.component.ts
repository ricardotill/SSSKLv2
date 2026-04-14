import { Component, ChangeDetectionStrategy, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { TagModule } from 'primeng/tag';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-branding',
  imports: [TagModule],
  template: `
    <div 
      class="flex items-center justify-center"
      [class.cursor-pointer]="authService.isAuthenticated()"
      (click)="onClick()"
      (keydown.enter)="onClick()"
      (keydown.space)="onClick()"
      [attr.role]="authService.isAuthenticated() ? 'button' : null"
      [attr.tabindex]="authService.isAuthenticated() ? 0 : null"
    >
      <h2 class="text-xl font-bold m-0">SSSKL</h2>
      @if (showVersion()) {
        <p-tag class="ml-2" value="v3.6.0" />
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BrandingComponent {
  authService = inject(AuthService);
  private router = inject(Router);
  
  showVersion = input<boolean>(true);

  onClick() {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/pos']);
    }
  }
}
