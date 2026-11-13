using Microsoft.AspNetCore.Identity;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;

namespace SSSKL.Web.Registrations;

/// <summary>
/// Realistic, fictional data for store screenshots (<c>Mobile/scripts/screenshots.sh</c>). Runs in the E2E
/// environment instead of <see cref="E2ESeeder"/> when <c>E2E:DemoData</c> is true, so nothing named "E2E"
/// shows up. Every user gets the password from <c>E2E:UserPassword</c>; <see cref="ScreenshotUser"/> is the
/// one the screenshots are taken as. Uses a fixed random seed, so every run produces the same data
/// (relative to today).
/// </summary>
internal static class DemoDataSeeder
{
    public const string ScreenshotUser = "lotte";

    private static readonly (string UserName, string Name, string Surname)[] Users =
    [
        (ScreenshotUser, "Lotte", "Vermeulen"),
        ("daan", "Daan", "Bakker"),
        ("sanne", "Sanne", "Jansen"),
        ("thijs", "Thijs", "Visser"),
        ("femke", "Femke", "Smit"),
        ("bram", "Bram", "Mulder"),
        ("noor", "Noor", "Bosman"),
        ("jesse", "Jesse", "Meijer"),
        ("eva", "Eva", "Hendriks"),
        ("luuk", "Luuk", "Dijkstra"),
    ];

    // Name, description, price, stock, how often it gets picked
    private static readonly (string Name, string Description, decimal Price, int Stock, int Weight)[] Products =
    [
        ("Cola", "Blikje 330 ml", 1.50m, 48, 6),
        ("Cola Zero", "Blikje 330 ml", 1.50m, 36, 4),
        ("Fanta", "Blikje 330 ml", 1.50m, 30, 3),
        ("Ice Tea", "Flesje 500 ml", 1.75m, 24, 3),
        ("Bier", "Flesje 300 ml", 2.00m, 96, 7),
        ("Radler 0.0%", "Flesje 300 ml", 2.00m, 24, 2),
        ("Chips", "Zakje naturel of paprika", 1.00m, 40, 4),
        ("Tosti", "Ham en kaas", 2.50m, 20, 3),
        ("Koffie", "Uit de automaat", 1.00m, 200, 3),
        ("Mars", "Reep", 1.25m, 30, 2),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var password = services.GetRequiredService<IConfiguration>()["E2E:UserPassword"];
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException("E2E:UserPassword must be configured to seed the demo users.");

        var random = new Random(20261113);
        var now = DateTime.UtcNow;
        var today = now.Date;

        var users = new List<ApplicationUser>();
        foreach (var (userName, name, surname) in Users)
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = $"{userName}@example.com",
                EmailConfirmed = true,
                Name = name,
                Surname = surname,
            };
            EnsureSucceeded(await userManager.CreateAsync(user, password), userName);
            EnsureSucceeded(await userManager.AddToRoleAsync(user, Roles.User), userName);
            users.Add(user);
        }
        var lotte = users[0];
        lotte.Description = "Leiding bij de Verkenners. Altijd in voor een tosti.";

        var products = Products
            .Select(p => new Product { Name = p.Name, Description = p.Description, Price = p.Price, Stock = p.Stock })
            .ToList();
        db.Product.AddRange(products);
        var weighted = Products
            .SelectMany((p, index) => Enumerable.Repeat(products[index], p.Weight))
            .ToList();

        // Orders on club evenings (Friday and Saturday) of the last ten weeks, plus the last few days for the
        // screenshot user so the streak and "recent" lists have something.
        for (var index = 0; index < users.Count; index++)
        {
            var user = users[index];
            var activity = index == 0 ? 0.9 : 0.35 + random.NextDouble() * 0.5;
            var orderDays = Enumerable.Range(0, 70)
                .Select(daysAgo => today.AddDays(-daysAgo))
                .Where(day => day.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday || (index == 0 && day > today.AddDays(-4)))
                .Where(_ => random.NextDouble() < activity);

            var orders = new List<Order>();
            foreach (var day in orderDays)
            {
                var count = random.Next(1, 5);
                for (var i = 0; i < count; i++)
                {
                    var product = weighted[random.Next(weighted.Count)];
                    var amount = random.NextDouble() < 0.8 ? 1 : 2;
                    var createdOn = day.AddHours(18).AddMinutes(random.Next(0, 300));
                    if (createdOn > now) createdOn = now.AddMinutes(-random.Next(5, 120));
                    orders.Add(new Order
                    {
                        User = user,
                        Product = product,
                        ProductNaam = product.Name,
                        Amount = amount,
                        Paid = product.Price * amount,
                        CreatedOn = createdOn,
                    });
                }
            }
            db.Order.AddRange(orders);

            // Top-ups in round amounts, before the orders they paid for; one member ends up slightly negative.
            var spent = orders.Sum(o => o.Paid);
            var target = index == 7 ? spent - 3.50m : spent + random.Next(4, 30) + (index == 0 ? 0.50m : 0m);
            var toppedUp = 0m;
            var topUpDay = today.AddDays(-72);
            while (toppedUp < target)
            {
                var amount = new[] { 10m, 20m, 25m, 50m }[random.Next(4)];
                if (toppedUp + amount > target + 25m) amount = 10m;
                db.TopUp.Add(new TopUp { User = user, Saldo = amount, CreatedOn = topUpDay.AddHours(19) });
                toppedUp += amount;
                topUpDay = topUpDay.AddDays(random.Next(7, 18));
                if (topUpDay > today) topUpDay = today.AddDays(-1);
            }

            user.Saldo = toppedUp - spent;
            user.LastOrdered = orders.Count == 0 ? now.AddDays(-30) : orders.Max(o => o.CreatedOn);
        }

        var events = SeedEvents(db, users, today, random);
        SeedQuotes(db, users, now);
        SeedAchievements(db, lotte, now);
        SeedNotifications(db, lotte, events, now);

        await db.SaveChangesAsync();

        // Stats, leaderboards and streaks come from the raw data, as after the admin's "recalculate".
        var stats = services.GetRequiredService<IUserStatRepository>();
        foreach (var user in users)
        {
            await stats.RecalculateByUserId(user.Id);
        }
    }

    private static List<Event> SeedEvents(ApplicationDbContext db, List<ApplicationUser> users, DateTime today, Random random)
    {
        (string Title, string Description, int InDays, int StartHour, int Hours, string Location)[] definitions =
        [
            ("Kampvuuravond",
                "<p>Marshmallows, broodjes en verhalen rond het vuur. Neem een warme trui mee!</p>",
                3, 19, 4, "Kampvuurkuil achter het clubhuis"),
            ("Pubquiz",
                "<p>Teams van vier, zes rondes en een wisselbeker. Opgeven kan tot vrijdag.</p>",
                10, 20, 3, "Clubhuis"),
            ("Pizza- en spelletjesavond",
                "<p>Bordspellen, pizza en de jaarlijkse Mario Kart-finale.</p>",
                17, 18, 5, "Clubhuis"),
            ("Klusdag",
                "<p>Samen het terrein winterklaar maken. Lunch wordt verzorgd.</p>",
                24, 10, 6, "Scoutingterrein"),
        ];

        var events = new List<Event>();
        foreach (var (title, description, inDays, startHour, hours, location) in definitions)
        {
            var start = today.AddDays(inDays).AddHours(startHour - 1); // UTC; shown an hour later in NL winter time
            var item = new Event
            {
                Title = title,
                Description = description,
                StartDateTime = start,
                EndDateTime = start.AddHours(hours),
                Creator = users[1],
                CreatorId = users[1].Id,
                LocationName = location,
                CreatedOn = today.AddDays(-random.Next(3, 20)),
            };
            foreach (var user in users.Where(_ => random.NextDouble() < 0.7))
            {
                item.Responses.Add(new EventResponse
                {
                    Event = item,
                    User = user,
                    UserId = user.Id,
                    Status = random.NextDouble() < 0.8 ? EventResponseStatus.Accepted : EventResponseStatus.Declined,
                });
            }
            events.Add(item);
        }
        db.Event.AddRange(events);

        AddComment(db, users[2], events[0], "Ik neem gitaar mee 🎸", today.AddDays(-2).AddHours(20));
        AddComment(db, users[0], events[0], "Ik zorg voor de marshmallows!", today.AddDays(-1).AddHours(19));
        AddComment(db, users[5], events[1], "Team Verkenners doet weer mee, we gaan die beker halen.", today.AddDays(-1).AddHours(21));
        return events;
    }

    private static void SeedQuotes(ApplicationDbContext db, List<ApplicationUser> users, DateTime now)
    {
        (string Text, int Author, int DaysAgo, int Votes)[] definitions =
        [
            ("Ik ben niet verdwaald, ik verken alleen een alternatieve route.", 3, 2, 7),
            ("Wie heeft het tosti-ijzer weer aan laten staan?", 1, 5, 4),
            ("Een tent opzetten in het donker is gewoon een teambuildingsoefening.", 0, 9, 6),
            ("Volgens mij is het kampvuur nu wel aan.", 5, 14, 3),
            ("Ik heb niet geslapen, ik heb mijn ogen van binnen gecontroleerd.", 8, 21, 5),
        ];

        foreach (var (text, author, daysAgo, votes) in definitions)
        {
            var createdOn = now.AddDays(-daysAgo).AddHours(-2);
            var creator = users[(author + 2) % users.Count];
            var quote = new Quote { Text = text, DateSaid = createdOn.AddHours(-1), CreatedById = creator.Id, CreatedBy = creator, CreatedOn = createdOn };
            quote.Authors.Add(new QuoteAuthor { Quote = quote, ApplicationUserId = users[author].Id });
            db.Quote.Add(quote);

            foreach (var voter in users.Where(u => u != users[author]).Take(votes))
            {
                db.QuoteVote.Add(new QuoteVote { Quote = quote, User = voter, UserId = voter.Id, CreatedOn = createdOn.AddHours(3) });
            }

            if (daysAgo == 2)
            {
                var comment = AddComment(db, users[4], quote, "Haha, dit was echt zo 😂", createdOn.AddHours(1));
                AddLike(db, users[0], comment, createdOn.AddHours(2));
                AddLike(db, users[6], comment, createdOn.AddHours(2));
            }
        }
    }

    private static void SeedAchievements(ApplicationDbContext db, ApplicationUser lotte, DateTime now)
    {
        (string Name, string Description, Achievement.ActionOption Action, int Value, Achievement.AchievementTier Tier, int EarnedDaysAgo)[] definitions =
        [
            ("Eerste rondje", "Plaats je eerste bestelling.", Achievement.ActionOption.UserOrderAmountBought, 1, Achievement.AchievementTier.Bronze, 68),
            ("Vaste klant", "Koop 50 producten.", Achievement.ActionOption.UserOrderAmountBought, 50, Achievement.AchievementTier.Silver, 12),
            ("Stamgast", "Koop 250 producten.", Achievement.ActionOption.UserOrderAmountBought, 250, Achievement.AchievementTier.Gold, -1),
            ("Spaarpot", "Waardeer in één keer € 50 op.", Achievement.ActionOption.UserIndividualTopUp, 50, Achievement.AchievementTier.Silver, 30),
            ("Op dreef", "Bestel drie dagen achter elkaar.", Achievement.ActionOption.CurrentStreak, 3, Achievement.AchievementTier.Bronze, 1),
            ("Grapjas", "Plaats je eerste quote.", Achievement.ActionOption.QuoteCount, 1, Achievement.AchievementTier.Bronze, 9),
            ("Publiekslieveling", "Krijg 25 stemmen op je quotes.", Achievement.ActionOption.QuoteVotesReceived, 25, Achievement.AchievementTier.Gold, -1),
            ("Kletskous", "Plaats 10 reacties.", Achievement.ActionOption.ReactionCount, 10, Achievement.AchievementTier.Bronze, -1),
            ("Veteraan", "Wees een jaar lid.", Achievement.ActionOption.YearsOfMembership, 1, Achievement.AchievementTier.Platinum, -1),
        ];

        foreach (var (name, description, action, value, tier, earnedDaysAgo) in definitions)
        {
            var achievement = new Achievement
            {
                Name = name,
                Description = description,
                AutoAchieve = false, // the seeded entries are the whole story; no awarding on startup
                Action = action,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = value,
                Tier = tier,
                CreatedOn = now.AddDays(-90),
            };
            db.Achievement.Add(achievement);
            if (earnedDaysAgo >= 0)
            {
                // HasSeen, so no achievement popup covers the screenshots.
                db.AchievementEntry.Add(new AchievementEntry
                {
                    Achievement = achievement,
                    User = lotte,
                    Tier = tier,
                    HasSeen = true,
                    CreatedOn = now.AddDays(-earnedDaysAgo).AddHours(-3),
                });
            }
        }
    }

    private static void SeedNotifications(ApplicationDbContext db, ApplicationUser lotte, List<Event> events, DateTime now)
    {
        (string Title, string Message, string? Link, int HoursAgo, bool Read)[] definitions =
        [
            ("Nieuw evenement", $"{events[1].Title} is gepland. Ben je erbij?", $"/events/{events[1].Id}", 3, false),
            ("Achievement behaald", "Je hebt 'Op dreef' behaald!", "/achievements", 20, false),
            ("Saldo opgewaardeerd", "Er is € 25,00 bij je saldo gezet.", "/orders/saldo", 50, true),
            ("Nieuwe reactie", "Femke reageerde op een quote.", "/quotes", 70, true),
            ("Nieuw evenement", $"{events[0].Title} is gepland. Ben je erbij?", $"/events/{events[0].Id}", 120, true),
        ];

        foreach (var (title, message, link, hoursAgo, read) in definitions)
        {
            db.Notification.Add(new Notification
            {
                User = lotte,
                UserId = lotte.Id,
                Title = title,
                Message = message,
                LinkUri = link,
                IsRead = read,
                CreatedOn = now.AddHours(-hoursAgo),
            });
        }
    }

    private static Reaction AddComment(ApplicationDbContext db, ApplicationUser user, BaseModel target, string text, DateTime createdOn)
    {
        var reaction = new Reaction
        {
            User = user,
            UserId = user.Id,
            Content = text,
            TargetId = target.Id,
            TargetType = target is Quote ? ReactionTargetType.Quote : ReactionTargetType.Event,
            CreatedOn = createdOn,
        };
        db.Reaction.Add(reaction);
        return reaction;
    }

    private static void AddLike(ApplicationDbContext db, ApplicationUser user, Reaction comment, DateTime createdOn) =>
        db.Reaction.Add(new Reaction
        {
            User = user,
            UserId = user.Id,
            Content = "👍",
            TargetId = comment.Id,
            TargetType = ReactionTargetType.Reaction,
            CreatedOn = createdOn,
        });

    private static void EnsureSucceeded(IdentityResult result, string userName)
    {
        if (result.Succeeded) return;
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Seeding demo user '{userName}' failed: {errors}");
    }
}
