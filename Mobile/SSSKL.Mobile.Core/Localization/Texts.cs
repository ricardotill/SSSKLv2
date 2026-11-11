namespace SSSKL.Mobile.Core.Localization;

/// <summary>
/// Dutch UI strings (the SPA is Dutch-only too; wording follows Frontend language.service.ts).
/// Static properties so XAML can bind them with x:Static.
/// </summary>
public static class Texts
{
    public static string AppName => "SSSKL";
    public static string TabPos => "Kassa";
    public static string TabProfile => "Profiel";
    public static string TabMore => "Meer";
    public static string TabNotifications => "Notificaties";

    // Notificaties
    public static string NotificationsSubtitle => "Bekijk je recente activiteit en reacties";
    public static string MarkAllRead => "Alles gelezen";
    public static string NoNotifications => "Nog geen notificaties";
    public static string NoNotificationsHint => "Wanneer je notificaties krijgt, verschijnen ze hier.";
    public static string MarkAllReadFailed => "Markeren als gelezen mislukt. Probeer het opnieuw.";
    public static string Unread => "Ongelezen";

    public static string Error => "Fout";
    public static string Ok => "OK";
    public static string Cancel => "Annuleren";
    public static string Close => "Sluiten";
    public static string LoadFailed => "Laden van gegevens mislukt";

    public static string Products => "Producten";
    public static string People => "Voor wie";
    public static string SearchPeople => "Zoek een persoon";
    public static string Amount => "Aantal";
    public static string Split => "Splitsen";
    public static string Total => "Totaal";
    public static string PerPerson => "p.p.";
    public static string PosSelectHint => "Kies wat en voor wie";
    public static string Order => "Bestellen";
    public static string OrderPlaced => "Order geplaatst!";
    public static string OrderFailed => "Bestelling mislukt";
    public static string GuestCannotOrder => "Gasten kunnen geen bestellingen plaatsen.";
    public static string OutOfStock => "Op";
    public static string You => "jij";

    public static string MySaldo => "Mijn saldo";
    public static string TotalOrders => "Bestellingen";
    public static string CurrentStreak => "Huidige streak";
    public static string Achievements => "Achievements";
    public static string NoAchievements => "Nog geen achievements behaald.";
    public static string ProfilePicture => "Profielfoto";
    public static string ChangeProfilePicture => "Profielfoto wijzigen";
    public static string RemoveProfilePicture => "Profielfoto verwijderen";
    public static string ConfirmRemoveProfilePicture => "Weet je zeker dat je je profielfoto wilt verwijderen?";
    public static string ProfilePictureUpdated => "Profielfoto bijgewerkt";
    public static string ProfilePictureRemoved => "Profielfoto verwijderd";
    public static string AboutMe => "Over mij";
    public static string Edit => "Bewerken";
    public static string NoDescription => "Je hebt nog niets over jezelf geschreven.";
    public static string DescriptionPlaceholder => "Vertel iets over jezelf…";
    public static string DescriptionSaved => "Je omschrijving is bijgewerkt.";
    public static string DescriptionSaveFailed => "Bijwerken van je omschrijving mislukt. Probeer het opnieuw.";
    public static string DescriptionLosesFormattingTitle => "Opmaak gaat verloren";
    public static string DescriptionLosesFormatting => "Je omschrijving heeft opmaak (zoals vet, lijsten of links) die de app niet kan bewaren. Bewerk hem op de website om die te houden.";
    public static string SaveAnyway => "Toch opslaan";
    public static string ProfilePictureFailed => "Profielfoto bijwerken mislukt";
    public static string Settings => "Instellingen";
    public static string Stats => "Statistieken";
    public static string MyOrders => "Mijn bestellingen";
    public static string Login => "Inloggen";
    public static string Retry => "Opnieuw proberen";
    public static string Logo => "SSSKL-logo";
    public static string MadeWithLoveBy => "Made with ❤️ by";
    public static string Author => "Ricardo Tillemans";
    public static string OpenAuthorWebsite => "Website van Ricardo Tillemans openen";
    public static string Logout => "Uitloggen";
    public static string ConfirmLogout => "Weet je zeker dat je wilt uitloggen?";

    public static string NewAchievementTitle => "🎉 Nieuwe achievement! 🎉";
    public static string NewAchievementsTitle => "🎉 Nieuwe achievements! 🎉";
    public static string NewAchievementDescription => "Je hebt zojuist nieuwe achievement(s) ontgrendeld!";
    public static string WhatsNewTitle => "Wat is er nieuw in SSSKL?";
    public static string SessionExpired => "Je sessie is verlopen. Log opnieuw in.";

    // "Meer"
    public static string SectionGeneral => "Algemeen";
    public static string SectionAdmin => "Beheer";
    public static string SectionApp => "App";
    public static string MySaldoPage => "Mijn saldo";
    public static string Notifications => "Notificaties";
    public static string Events => "Evenementen";
    public static string Quotes => "Quotes";
    public static string Leaderboard => "Leaderboard";
    public static string SaldoList => "Saldolijst";
    public static string About => "Over SSSKL";
    public static string AdminOrders => "Bestellingen";
    public static string AdminProducts => "Producten";
    public static string AdminTopUps => "Opwaarderingen";
    public static string AdminUsers => "Gebruikers";
    public static string AdminRoles => "Rollen";
    public static string AdminAchievements => "Achievements";
    public static string AdminAnnouncements => "Mededelingen";
    public static string AdminSendNotification => "Notificatie verzenden";
    public static string AdminGlobalSettings => "Globale instellingen";
    public static string ViewProfile => "Bekijk je profiel";

    // Settings
    public static string SectionProfile => "Profiel";
    public static string SectionAppearance => "Weergave";
    public static string SectionAccount => "Account";
    public static string SectionAbout => "Over";
    public static string FirstName => "Voornaam";
    public static string LastName => "Achternaam";
    public static string PhoneNumber => "Telefoonnummer";
    public static string Save => "Opslaan";
    public static string SettingsSaved => "Je instellingen zijn bijgewerkt.";
    public static string SettingsSaveFailed => "Bijwerken van instellingen mislukt. Probeer het opnieuw.";
    public static string Roles => "Rollen";
    public static string NoRoles => "Geen rollen";
    public static string ColorMode => "Kleurmodus";
    public static string ColorModeHint => "'Systeem' volgt de instelling van je telefoon.";
    public static string ThemeAuto => "Systeem";
    public static string ThemeLight => "Licht";
    public static string ThemeDark => "Donker";
    public static string Email => "E-mailadres";
    public static string EmailConfirmed => "Bevestigd";
    public static string EmailUnconfirmed => "Onbevestigd";
    public static string PasswordAndPasskeys => "Wachtwoord en passkeys";
    public static string TwoFactor => "Tweestapsverificatie";
    public static string On => "Aan";
    public static string Off => "Uit";
    public static string PersonalData => "Persoonlijke gegevens";
    public static string AppVersion => "Versie";
}
