import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CardModule } from 'primeng/card';
import { BrandingComponent } from '../../shared/components/branding/branding.component';
import { NgOptimizedImage } from '@angular/common';

@Component({
  selector: 'app-homepage',
  standalone: true,
  imports: [CardModule, BrandingComponent, NgOptimizedImage],
  template: `
    <div class="flex justify-center items-center h-full sm:mt-10">
      <p-card class="max-w-2xl w-full" styleClass="homepage-card">
        <div class="flex flex-col sm:flex-row items-center sm:items-start gap-6">
          <img
            ngSrc="/logo.png"
            alt="SSSKL Logo"
            width="144"
            height="144"
            priority
            class="rounded-xl shadow-lg"
          />
          <div class="flex flex-col justify-center text-center sm:text-left h-36">
            <app-branding class="scale-150 origin-center sm:origin-left mb-4" />
            <p class="m-0 text-muted-color">
              Made with ❤️ by
              <a
                href="https://ricardotill.nl"
                target="_blank"
                class="font-medium hover:underline text-primary"
                >Ricardo Tillemans</a
              >
            </p>
          </div>
        </div>

        <div class="mt-12 pt-6 border-t border-surface-border">
          <p class="text-sm leading-relaxed text-muted-color mb-0">
            Wil je jouw account verwijderen? Log dan in, open het menu, open Accountbeheer, ga naar
            het kopje Persoonlijke data en bevestig dat je jouw account wilt verwijderen. Kan je
            niet meer inloggen? Stuur dan een mail naar
            <a href="mailto:me@ricardotill.nl" class="font-medium hover:underline text-primary"
              >me@ricardotill.nl</a
            >
          </p>
        </div>
      </p-card>
    </div>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }

    :host ::ng-deep .homepage-card {
      background: var(--p-surface-900);
      border: 1px solid var(--p-surface-800);
    }

    @media (prefers-color-scheme: light) {
      :host ::ng-deep .homepage-card {
        background: var(--p-surface-50);
        border: 1px solid var(--p-surface-200);
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class HomepageComponent {}
