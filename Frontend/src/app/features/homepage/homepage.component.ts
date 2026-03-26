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
      <p-card class="max-w-2xl w-full" styleClass="bg-surface-50 border border-surface-200 dark:bg-surface-900 dark:border-surface-800">
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
            
            <div class="mt-8">
              <a 
                href="https://github.com/ricardotill/SSSKLv2" 
                target="_blank" 
                class="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-300 rounded-full hover:bg-surface-200 dark:hover:bg-surface-700 transition-colors shadow-sm ring-1 ring-surface-200 dark:ring-surface-700 hover:ring-primary/50"
              >
                <i class="pi pi-github text-lg"></i>
                View on GitHub
              </a>
            </div>
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
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class HomepageComponent {}
