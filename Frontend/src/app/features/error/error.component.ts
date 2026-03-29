import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { trigger, transition, style, animate, state } from '@angular/animations';

@Component({
  selector: 'app-error',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonModule],
  template: `
    <div class="error-container">
      <div class="glass-orb orb-1"></div>
      <div class="glass-orb orb-2"></div>
      
      <div class="content" [@fadeInUp]>
        <h1 class="error-code text-gradient">{{ errorCode }}</h1>
        <h2 class="error-title">{{ errorTitle }}</h2>
        <p class="error-message">{{ errorMessage }}</p>
        
        <div class="actions">
          <button pButton pRipple 
                  label="Terug naar Home" 
                  icon="pi pi-home" 
                  class="p-button-rounded p-button-raised p-button-lg gradient-button"
                  routerLink="/"></button>
        </div>
      </div>
      
      <div class="footer-msg">
        <p>Als dit blijft gebeuren, neem contact op met <a href="mailto:webmaster@scoutingwilo.nl">webmaster@scoutingwilo.nl</a></p>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100vh;
      overflow: hidden;
    }

    .error-container {
      position: relative;
      height: 100%;
      width: 100%;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: var(--p-surface-50);
      color: var(--p-surface-900);
      font-family: var(--font-family, 'Inter', sans-serif);
      text-align: center;
      padding: 2rem;
      transition: background 0.3s ease;
    }

    :host-context(.dark) .error-container {
      background: var(--p-surface-950);
      color: var(--p-surface-0);
    }

    .glass-orb {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      z-index: 1;
      opacity: 0.4;
    }

    .orb-1 {
      width: 400px;
      height: 400px;
      /* Emerald translucent */
      background: rgba(16, 185, 129, 0.25);
      top: -100px;
      right: -100px;
      animation: float 10s infinite alternate ease-in-out;
    }

    .orb-2 {
      width: 300px;
      height: 300px;
      /* Teal translucent */
      background: rgba(20, 184, 166, 0.2);
      bottom: -50px;
      left: -50px;
      animation: float 12s infinite alternate-reverse ease-in-out;
    }

    .content {
      position: relative;
      z-index: 10;
      max-width: 600px;
    }

    .error-code {
      font-size: 10rem;
      font-weight: 900;
      line-height: 1;
      margin: 0;
      letter-spacing: -0.05em;
    }

    .text-gradient {
      /* Emerald to Primary gradient */
      background: linear-gradient(135deg, var(--p-primary-400) 0%, var(--p-primary-600) 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }

    .error-title {
      font-size: 2.5rem;
      font-weight: 700;
      margin: 1rem 0;
      color: var(--p-surface-900);
    }

    :host-context(.dark) .error-title {
      color: var(--p-surface-0);
    }

    .error-message {
      font-size: 1.25rem;
      color: var(--p-surface-600);
      margin-bottom: 2.5rem;
      line-height: 1.6;
    }

    :host-context(.dark) .error-message {
      color: var(--p-surface-400);
    }

    .gradient-button {
      /* Standard primary button feel but with the custom gradient look */
      background: linear-gradient(135deg, var(--p-primary-500) 0%, var(--p-primary-700) 100%) !important;
      border: none !important;
      padding: 1rem 2rem !important;
      font-weight: 600 !important;
      transition: transform 0.2s, box-shadow 0.2s !important;
    }

    .gradient-button:hover {
      transform: translateY(-2px);
      box-shadow: 0 10px 25px -5px rgba(16, 185, 129, 0.4) !important;
    }

    .footer-msg {
      position: absolute;
      bottom: 2rem;
      color: var(--p-surface-500);
      font-size: 0.875rem;
    }

    .footer-msg a {
      color: var(--p-primary-500);
      text-decoration: none;
      font-weight: 500;
    }

    .footer-msg a:hover {
      text-decoration: underline;
    }

    @keyframes float {
      0% { transform: translate(0, 0) scale(1); }
      100% { transform: translate(30px, 40px) scale(1.1); }
    }

    @media (max-width: 640px) {
      .error-code { font-size: 6rem; }
      .error-title { font-size: 1.75rem; }
    }
  `],
  animations: [
    trigger('fadeInUp', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(40px)' }),
        animate('800ms cubic-bezier(0.2, 0.8, 0.2, 1)',
          style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class ErrorComponent implements OnInit {
  errorCode: string = '404';
  errorTitle: string = 'Pagina niet gevonden';
  errorMessage: string = 'Oeps! Het lijkt erop dat de pagina die je zoekt niet bestaat of verplaatst is.';

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const code = params['code'];
      if (code === '500') {
        this.errorCode = '500';
        this.errorTitle = 'Interne Serverfout';
        this.errorMessage = 'Er is iets misgegaan op onze servers. We zijn ervan op de hoogte e werken aan een oplossing.';
      } else if (code === '403') {
        this.errorCode = '403';
        this.errorTitle = 'Toegang Geweigerd';
        this.errorMessage = 'Je hebt geen rechten om deze pagina te bekijken.';
      }
    });

    // Check if we are on a "real" error path based on route data if needed
    const dataCode = this.route.snapshot.data['code'];
    if (dataCode) {
      this.setErrorCode(dataCode);
    }
  }

  private setErrorCode(code: string) {
    if (code === '404') {
      this.errorCode = '404';
      this.errorTitle = 'Pagina niet gevonden';
      this.errorMessage = 'Oeps! Het lijkt erop dat de pagina die je zoekt niet bestaat of verplaatst is.';
    } else if (code === '500') {
      this.errorCode = '500';
      this.errorTitle = 'Serverfout';
      this.errorMessage = 'Er is een onverwachte fout opgetreden. Probeer het later opnieuw.';
    }
  }
}
export default ErrorComponent;
