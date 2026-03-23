import { Injectable, signal, computed } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  // Current language - defaulting to Dutch as requested
  language = signal<'nl' | 'en'>('nl');

  // Centralized Dutch translations
  private readonly dutchTranslations = {
    // General
    'success': 'Succes',
    'error': 'Fout',
    'confirm': 'Bevestigen',
    'cancel': 'Annuleren',
    'save': 'Opslaan',
    'delete': 'Verwijderen',
    'refresh': 'Vernieuwen',
    'loading': 'Laden...',
    'actions': 'Acties',
    'edit': 'Bewerken',
    'never': 'Nooit',

    // Layout
    'welcome': 'Welkom',
    'login': 'Inloggen',
    'register': 'Registreren',
    'logout': 'Uitloggen',
    'main': 'Hoofdmenu',
    'homepage': 'Home',
    'about': 'Over',
    'user': 'Gebruiker',
    'my_orders': 'Mijn Bestellingen',
    'my_saldo': 'Mijn Saldo',
    'settings': 'Instellingen',
    'admin': 'Administratie',
    'users': 'Gebruikers',
    'user_overview': 'Gebruikers',
    'products': 'Producten',
    'announcements': 'Mededelingen',
    'top_ups': 'Opwaarderingen',
    'orders': 'Bestellingen',
    'pos': 'Bestellen',

    // POS
    'what': 'Wat',
    'who': 'Wie',
    'pay': 'Betalen',
    'split_bill': 'Rekening splitten?',
    'amount': 'Aantal',
    'order': 'Bestellen',
    'order_placed': 'Order geplaatst!',
    'order_failed': 'Bestelling mislukt',
    'load_failed': 'Laden van gegevens mislukt',

    // Orders
    'date': 'Datum',
    'product': 'Product',
    'price': 'Prijs',
    'no_orders': 'Geen bestellingen gevonden.',
    'export_csv': 'CSV',
    'confirm_export_csv_title': 'Exporteren Bevestigen',
    'confirm_export_csv_message': 'Weet je zeker dat je alle bestellingen wilt exporteren naar een CSV-bestand?',
    'confirm_delete_order': 'Weet je zeker dat je deze bestelling voor {product} wilt verwijderen? Dit kan niet ongedaan worden gemaakt.',
    'confirm_delete_title': 'Verwijdering Bevestigen',
    'order_deleted': 'Bestelling succesvol verwijderd',
    'showing_orders_report': 'Bestellingen {first} t/m {last} van {total}',
    'showing_orders_report_no_total': 'Bestellingen {first} t/m {last}',
    'total_users': 'Totaal Gebruikers',
    'revenue': 'Omzet',
    'active_sessions': 'Actieve Sessies',
    'admin_orders': 'Bestellingen',
    'admin_orders_desc': 'Beheer hier alle systeembestellingen.',
    'announcements_desc': 'Beheer hier de mededelingen.',
    'products_desc': 'Beheer hier de producten.',
    'add_product': 'Product Toevoegen',
    'edit_product': 'Product Bewerken',
    'product_name': 'Productnaam',
    'product_description': 'Omschrijving',
    'stock': 'Voorraad',
    'product_added': 'Product succesvol toegevoegd',
    'product_updated': 'Product succesvol bijgewerkt',
    'product_deleted': 'Product succesvol verwijderd',
    'confirm_delete_product': 'Weet je zeker dat je product {name} wilt verwijderen? Dit kan niet ongedaan worden gemaakt.',
    'no_products': 'Geen producten gevonden.',
    'top_ups_desc': 'Beheer hier de opwaarderingen.',
    'no_top_ups': 'Geen opwaarderingen gevonden.',
    'showing_top_ups_report': 'Opwaarderingen {first} t/m {last} van {total}',
    'add_top_up': 'Opwaardering Toevoegen',
    'top_up_added': 'Opwaardering succesvol toegevoegd',
    'top_up_deleted': 'Opwaardering succesvol verwijderd',
    'confirm_delete_top_up': 'Weet je zeker dat je deze opwaardering wilt verwijderen?',
    'select_user': 'Selecteer Gebruiker',

    // Admin - Users
    'username': 'Gebruikersnaam',
    'name': 'Naam',
    'full_name': 'Volledige Naam',
    'balance': 'Saldo',
    'last_ordered': 'Laatst Besteld',
    'edit_user': 'Gebruiker Bewerken',
    'first_name': 'Voornaam',
    'last_name': 'Achternaam',
    'email_confirmed': 'E-mail Bevestigd',
    'phone_number': 'Telefoonnummer',
    'phone_number_confirmed': 'Telefoonnummer Bevestigd',
    'new_password_help': 'Nieuw wachtwoord (leeg laten om huidig te behouden)',
    'roles': 'Rollen',
    'select_roles': 'Selecteer Rollen',
    'user_updated': 'Gebruiker succesvol bijgewerkt',
    'update_failed': 'Bijwerken gebruiker mislukt',
    'confirm_delete_user': 'Weet je zeker dat je gebruiker {user} wilt verwijderen? Dit kan niet ongedaan worden gemaakt.',
    'user_deleted': 'Gebruiker succesvol verwijderd',
    'delete_failed': 'Verwijderen gebruiker mislukt',
    'no_users': 'Geen gebruikers gevonden.',
    'email': 'E-mail',
    'password_edit_desc': 'Nieuw wachtwoord (laat leeg om te behouden)',

    // Admin - Announcements
    'add_announcement': 'Mededeling Toevoegen',
    'edit_announcement': 'Mededeling Bewerken',
    'message': 'Bericht',
    'announcement_order': 'Volgorde',
    'is_scheduled': 'Ingepland',
    'planned_from': 'Gepland van',
    'planned_till': 'Gepland tot',
    'announcement_added': 'Mededeling succesvol toegevoegd',
    'announcement_updated': 'Mededeling succesvol bijgewerkt',
    'announcement_deleted': 'Mededeling succesvol verwijderd',
    'confirm_delete_announcement': 'Weet je zeker dat je deze mededeling wilt verwijderen? Dit kan niet ongedaan worden gemaakt.',
    'no_announcements': 'Geen mededelingen gevonden.',

    // Settings
    'user_settings': 'Gebruikersinstellingen',
    'profile': 'Profiel',
    'security': 'Accountbeveiliging',
    'tfa': '2FA Instellingen',
    'personal_data': 'Persoonlijke Gegevens',
    'no_roles': 'Geen rollen toegewezen',
    'color_mode': 'Website Kleurmodus',
    'color_mode_desc': "Kies hoe de website eruitziet. 'Auto' volgt je systeeminstellingen.",
    'save_changes': 'Wijzigingen Opslaan',
    'settings_updated': 'Je instellingen zijn bijgewerkt.',
    'settings_update_failed': 'Bijwerken van instellingen mislukt. Probeer het opnieuw.',

    // Settings - Security
    'email_status': 'Huidige Email Status',
    'confirmed': 'Bevestigd',
    'unconfirmed': 'Onbevestigd',
    'new_email': 'Nieuw E-mailadres',
    'email_placeholder': 'Laat leeg om huidig e-mailadres te behouden',
    'current_password': 'Huidig Wachtwoord',
    'password_placeholder': 'Vereist bij het wijzigen van je wachtwoord',
    'new_password': 'Nieuw Wachtwoord',
    'new_password_placeholder': 'Laat leeg om huidig wachtwoord te behouden',
    'update_security': 'Beveiliging Bijwerken',
    'security_updated': 'Je beveiligingsinstellingen zijn bijgewerkt.',
    'security_update_failed': 'Bijwerken van beveiligingsinstellingen mislukt. Controleer je invoer.',

    // Settings - 2FA
    'enable_tfa': 'Tweestapsverificatie Inschakelen',
    'tfa_desc': 'Beveilig je account door 2FA in te schakelen. Open je authenticator app (zoals Google Authenticator of Authy) en scan de QR-code hieronder, of voer de gedeelde sleutel handmatig in.',
    'shared_key': 'Gedeelde Sleutel',
    'verify_code': 'Code Verifiëren',
    'digit_code': '6-cijferige code',
    'verify_enable': 'Verifiëren & Inschakelen',
    'tfa_enabled_title': 'Tweestapsverificatie is Ingeschakeld',
    'tfa_enabled_desc': 'Je account is beveiligd. Je hebt nog <strong>{count}</strong> herstelcodes over.',
    'save_recovery_codes': '⚠️ Bewaar deze herstelcodes',
    'recovery_codes_desc': 'Deze codes worden niet opnieuw getoond. Bewaar ze op een veilige plek zoals een wachtwoordmanager.',
    'disable_tfa': '2FA Uitschakelen',
    'reset_recovery': 'Herstelcodes Opnieuw Instellen',
    'tfa_success': 'Tweestapsverificatie succesvol ingeschakeld.',
    'tfa_failed': 'Inschakelen van 2FA mislukt. Controleer je code en probeer het opnieuw.',
    'tfa_disabled': 'Tweestapsverificatie uitgeschakeld.',
    'recovery_reset_success': 'Herstelcodes succesvol opnieuw ingesteld.',

    // Settings - Personal Data
    'download_data_title': 'Persoonlijke Gegevens Downloaden',
    'download_data_desc': 'Je kunt een kopie opvragen van je persoonlijke gegevens die aan je account zijn gekoppeld. Dit gedownloade bestand bevat alle informatie die we veilig over jou opslaan.',
    'download_btn': 'Mijn Gegevens Downloaden',
    'delete_account_title': 'Account Verwijderen',
    'delete_account_desc': 'Zodra je je account verwijdert, is er geen weg terug. Wees alsjeblieft zeker. Al je gegevens inclusief bestelgeschiedenis worden permanent verwijderd.',
    'delete_account_btn': 'Mijn Account Verwijderen',
    'confirm_delete_account': 'Weet je zeker dat je je account wilt verwijderen? Deze actie kan niet ongedaan worden gemaakt.',
    'confirm_delete_account_title': 'Accountverwijdering Bevestigen',
    'account_deleted': 'Je account is verwijderd.',
  };

  // Signal for getting translations
  t = computed(() => {
    // For now we only have Dutch version here, but structure allows adding more
    return this.dutchTranslations;
  });

  constructor() { }

  // Basic translate function
  translate(key: keyof typeof this.dutchTranslations, params?: Record<string, string | number>): string {
    let text = this.dutchTranslations[key] || key;
    if (params) {
      Object.keys(params).forEach(p => {
        text = text.replace(`{${p}}`, params[p].toString());
      });
    }
    return text;
  }
}
