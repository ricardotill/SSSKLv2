import { Component, signal, inject, effect } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/theme.service';
import { PrimeNG } from 'primeng/config';
import { AchievementModalComponent } from './shared/components/achievement-modal/achievement-modal.component';
import { AuthService } from './core/auth/auth.service';
import { AchievementPopupService } from './features/achievements/services/achievement-popup.service';
import { WhatsNewModalComponent } from './shared/components/whats-new-modal/whats-new-modal.component';
import { UserProfileDrawerComponent } from './shared/components/user-profile-drawer/user-profile-drawer.component';
import { PushPermissionDialogComponent } from './layout/push-permission-dialog/push-permission-dialog.component';
import { PushNotificationService } from './core/services/push-notification.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AchievementModalComponent, WhatsNewModalComponent, UserProfileDrawerComponent, PushPermissionDialogComponent, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('SSSKLv2');
  private themeService = inject(ThemeService);
  private primeng = inject(PrimeNG);
  private authService = inject(AuthService);
  private popupService = inject(AchievementPopupService);
  protected pushService = inject(PushNotificationService);

  constructor() {
    this.themeService.init();
    this.configureDutchPrimeNG();

    // Check for unseen achievements whenever the current user changes (login, app init if authenticated)
    // Use an effect to reactively check when user session establishes
    effect(() => {
      const user = this.authService.currentUser();
      if (user) {
        // Small delay to ensure any redirects (like login -> pos) finish before triggering modal
        setTimeout(() => {
          this.popupService.checkUnseenAchievements();
        }, 500);
      }
    });
  }

  private configureDutchPrimeNG() {
    this.primeng.setTranslation({
      accept: 'Ja',
      reject: 'Nee',
      choose: 'Kies',
      upload: 'Uploaden',
      cancel: 'Annuleren',
      dayNames: ["Zondag", "Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag", "Zaterdag"],
      dayNamesShort: ["Zon", "Maan", "Din", "Woe", "Don", "Vrij", "Zat"],
      dayNamesMin: ["Zo", "Ma", "Di", "Wo", "Do", "Vr", "Za"],
      monthNames: ["Januari", "Februari", "Maart", "April", "Mei", "Juni", "Juli", "Augustus", "September", "Oktober", "November", "December"],
      monthNamesShort: ["Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dec"],
      today: 'Vandaag',
      clear: 'Wissen',
      weekHeader: 'Wk',
      firstDayOfWeek: 1,
      dateFormat: 'dd-mm-yy',
      weak: 'Zwak',
      medium: 'Gemiddeld',
      strong: 'Sterk',
      passwordPrompt: 'Voer een wachtwoord in',
      emptyFilterMessage: 'Geen resultaten gevonden',
      emptyMessage: 'Geen opties beschikbaar',
      aria: {
        trueLabel: 'Waar',
        falseLabel: 'Onwaar',
        nullLabel: 'Niet geselecteerd',
        star: '1 ster',
        stars: '{star} sterren',
        selectAll: 'Selecteer alle items',
        unselectAll: 'Deselecteer alle items',
        close: 'Sluiten',
        previous: 'Vorige',
        next: 'Volgende',
        navigation: 'Navigatie',
        scrollTop: 'Scroll naar boven',
        moveTop: 'Verplaats naar boven',
        moveUp: 'Verhoog',
        moveDown: 'Verlaag',
        moveBottom: 'Verplaats naar beneden',
        moveToTarget: 'Verplaats naar doel',
        moveToSource: 'Verplaats naar bron',
        moveAllToTarget: 'Verplaats alles naar doel',
        moveAllToSource: 'Verplaats alles naar bron',
        pageLabel: 'Pagina {page}',
        firstPageLabel: 'Eerste Pagina',
        lastPageLabel: 'Laatste Pagina',
        nextPageLabel: 'Volgende Pagina',
        prevPageLabel: 'Vorige Pagina',
        rowsPerPageLabel: 'Rijen per pagina',
        jumpToPageDropdownLabel: 'Spring naar pagina dropdown',
        jumpToPageInputLabel: 'Spring naar pagina input',
        selectRow: 'Rij geselecteerd',
        unselectRow: 'Rij gedeselecteerd',
        expandRow: 'Rij uitgeklapt',
        collapseRow: 'Rij ingeklapt',
        showFilterMenu: 'Toon filtermenu',
        hideFilterMenu: 'Verberg filtermenu',
        filterOperator: 'Filter operator',
        editRow: 'Bewerk rij',
        saveEdit: 'Sla bewerking op',
        cancelEdit: 'Annuleer bewerking',
        listView: 'Lijstweergave',
        gridView: 'Rasterweergave',
        slide: 'Dia',
        slideNumber: '{slideNumber}',
        zoomImage: 'Zoom afbeelding',
        zoomIn: 'Inzoomen',
        zoomOut: 'Uitzoomen',
        rotateRight: 'Roteer rechts',
        rotateLeft: 'Roteer links'
      }
    });
  }
}
