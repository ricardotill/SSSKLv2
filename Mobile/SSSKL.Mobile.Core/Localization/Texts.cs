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

    // Lists
    public static string Search => "Zoeken";
    public static string Delete => "Verwijderen";

    // Mijn bestellingen / Mijn saldo / Saldolijst
    public static string NoOrders => "Geen bestellingen gevonden.";
    public static string ConfirmDeleteOrderTitle => "Bestelling verwijderen";
    public static string ConfirmDeleteOrder(string product) =>
        $"Weet je zeker dat je deze bestelling voor {product} wilt verwijderen? Dit kan niet ongedaan worden gemaakt.";
    public static string OrderDeleted => "Bestelling verwijderd";
    public static string OrderDeleteFailed => "Verwijderen van de bestelling mislukt.";
    public static string TopUps => "Opwaarderingen";
    public static string NoTopUps => "Geen opwaarderingen gevonden.";
    public static string SearchMembers => "Zoek een lid";
    public static string NoUsers => "Geen gebruikers gevonden.";

    // Leaderboard
    public static string Last12Hours => "12 uur";
    public static string ThisMonth => "Maand";
    public static string AllTime => "Totaal";
    public static string Product => "Product";
    public static string NoPurchasesPeriod => "Er zijn deze periode geen aankopen gedaan. Dus ik zou zeggen, bestellen maar! ;)";
    public static string Rank(int position) => $"Plek {position}";

    // Statistieken
    public static string MyStats => "Mijn statistieken";
    public static string StatsOf(string name) => $"Statistieken van {name}";
    public static string MyStatsSubtitle => "Een volledig overzicht van jouw activiteit op SSSKL.";
    public static string StatsSubtitle => "Een volledig overzicht van de activiteit van deze gebruiker op SSSKL.";
    public static string RecalculateStats => "Statistieken herberekenen";
    public static string LastRecalculated(string when) => $"Laatste herberekening: {when}";
    public static string StatsRecalculated => "Je statistieken zijn herberekend.";
    public static string StatsRecalculateTooSoon => "Je kunt statistieken maar eenmaal per zeven dagen herberekenen.";
    public static string StatsRecalculateFailed => "Statistieken konden niet worden herberekend.";
    public static string Spent => "Uitgegeven";
    public static string Streak => "Streak";
    public static string Days(int days) => days == 1 ? "1 dag" : $"{days} dagen";
    public static string SectionOrdersAndSaldo => "Bestellingen en saldo";
    public static string SectionCommunity => "Community";
    public static string SectionRecords => "Persoonlijke records";
    public static string StatTotalOrders => "Totale bestellingen";
    public static string StatItemsBought => "Gekochte items";
    public static string StatTotalSpent => "Totaal besteed";
    public static string StatTotalTopUp => "Totale opwaardering";
    public static string StatMaxSingleTopUp => "Grootste losse opwaardering";
    public static string StatMaxOrdersPerHour => "Meeste bestellingen per uur";
    public static string StatQuotes => "Quotes geplaatst";
    public static string StatQuoteVotesGiven => "Quotes gestemd";
    public static string StatQuoteVotesReceived => "Ontvangen quote-stemmen";
    public static string StatReactions => "Reacties geplaatst";
    public static string StatMemberSince => "Lid sinds";
    public static string StatMinBetweenOrders => "Kortste tijd tussen bestellingen";
    public static string StatMinBetweenTopUps => "Kortste tijd tussen opwaarderingen";
    public static string StatLastActivity => "Laatste activiteit";
    public static string StatLastOrder => "Laatste bestelling";
    public static string StatLastTopUp => "Laatste opwaardering";
    public static string NotHappenedYet => "Nog niet gebeurd";
    public static string NoData => "Geen gegevens";
    public static string Minutes(int minutes) => $"{minutes} min.";
    public static string ViewAll => "Alles bekijken";

    // Achievements
    public static string AchievementsSubtitle => "Bekijk welke achievements je hebt behaald.";
    public static string AchievementsOf(string name) => $"Achievements van {name}";
    public static string FilterAll => "Alle";
    public static string FilterEarned => "Behaald";
    public static string FilterLocked => "Nog niet";
    public static string EarnedCount(int earned, int total) => $"{earned} van {total} behaald";
    public static string AchievedOn(string date) => $"Behaald op {date}";
    public static string NotAchievedYet => "Nog niet behaald";
    public static string Locked => "Vergrendeld";
    public static string PartOf(string name) => $"Onderdeel van {name}";
    public static string EarnedBy => "Behaald door";
    public static string EarnedByCount(int count) => count == 1 ? "Behaald door 1 lid" : $"Behaald door {count} leden";
    public static string NoEarners => "Niemand heeft dit achievement nog behaald.";
    public static string AchievementNotFound => "Dit achievement bestaat niet of is niet gevonden.";
    public static string TierBronze => "Brons";
    public static string TierSilver => "Zilver";
    public static string TierGold => "Goud";
    public static string TierPlatinum => "Platina";

    // Profiel van een ander lid
    public static string NoDescriptionOther => "Dit lid heeft nog geen beschrijving toegevoegd.";
    public static string RecentAchievements => "Recente achievements";
    public static string ViewStats => "Statistieken bekijken";

    // Over SSSKL
    public static string ViewOnGitHub => "Bekijk op GitHub";
    public static string DeleteAccountInfo =>
        "Wil je jouw account verwijderen? Ga naar Instellingen, open Persoonlijke gegevens en bevestig dat je jouw account wilt verwijderen. Kun je niet meer inloggen? Stuur dan een mail naar";
    public static string ContactEmail => "me@ricardotill.nl";

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
    /// <summary>Your own row in Kassa's "Voor wie" list.</summary>
    public static string Yourself => "Jezelf";

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
