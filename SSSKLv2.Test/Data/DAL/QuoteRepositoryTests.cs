using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class QuoteRepositoryTests : RepositoryTest
{
    private ApplicationDbContext _context = null!;
    private QuoteRepository _repository = null!;
    private IdentityRole _leiding = null!;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Init()
    {
        InitializeDatabase();
        _context = new ApplicationDbContext(GetOptions());
        _repository = new QuoteRepository(_context);

        _leiding = new IdentityRole("Leiding") { NormalizedName = "LEIDING" };
        _context.Roles.Add(_leiding);
        _context.SaveChanges();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Dispose();
        CleanupDatabase();
    }

    private Quote NewQuote(string text, DateTime dateSaid, bool restricted = false, string? authorId = null)
    {
        var quote = new Quote
        {
            Text = text,
            DateSaid = dateSaid,
            CreatedById = TestUser.Id,
            Authors = new List<QuoteAuthor>
            {
                new() { ApplicationUserId = authorId, CustomName = authorId == null ? "Opa" : null },
                new() { CustomName = "Oma" },
            },
        };
        if (restricted)
        {
            quote.VisibleToRoles.Add(_leiding);
        }
        return quote;
    }

    [TestMethod]
    public async Task GetAll_ReturnsQuotesNewestFirstWithAuthorsAndRoles()
    {
        await _repository.Add(NewQuote("Oud", new DateTime(2025, 1, 1)));
        await _repository.Add(NewQuote("Nieuw", new DateTime(2026, 1, 1), restricted: true));

        var (items, total) = await _repository.GetAll(isAdmin: true);

        total.Should().Be(2);
        var list = items.ToList();
        list.Select(q => q.Text).Should().Equal("Nieuw", "Oud");
        list[0].Authors.Should().HaveCount(2);
        list[0].VisibleToRoles.Should().ContainSingle(r => r.Name == "Leiding");
        list[0].CreatedBy!.Id.Should().Be(TestUser.Id);
    }

    [TestMethod]
    public async Task GetAll_HidesRestrictedQuotesFromUsersWithoutTheRole()
    {
        await _repository.Add(NewQuote("Open", new DateTime(2025, 1, 1)));
        await _repository.Add(NewQuote("Geheim", new DateTime(2026, 1, 1), restricted: true));

        var (asGuest, guestTotal) = await _repository.GetAll(userRoles: new List<string> { "User" });
        var (asLeiding, leidingTotal) = await _repository.GetAll(userRoles: new List<string> { "Leiding" });

        guestTotal.Should().Be(1);
        asGuest.Single().Text.Should().Be("Open");
        leidingTotal.Should().Be(2);
        asLeiding.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task GetAll_FiltersByAuthorAndPages()
    {
        await _repository.Add(NewQuote("Van mij 1", new DateTime(2025, 1, 1), authorId: TestUser.Id));
        await _repository.Add(NewQuote("Van mij 2", new DateTime(2025, 2, 1), authorId: TestUser.Id));
        await _repository.Add(NewQuote("Van een ander", new DateTime(2025, 3, 1)));

        var (items, total) = await _repository.GetAll(skip: 1, take: 1, isAdmin: true, targetUserId: TestUser.Id);

        total.Should().Be(2);
        items.Single().Text.Should().Be("Van mij 1");
    }

    [TestMethod]
    public async Task GetById_ReturnsTheQuoteWithItsRelations()
    {
        var quote = NewQuote("Hoi", new DateTime(2025, 1, 1), restricted: true, authorId: TestUser.Id);
        await _repository.Add(quote);

        var result = await _repository.GetById(quote.Id);

        result.Should().NotBeNull();
        result!.Authors.Should().Contain(a => a.ApplicationUser != null && a.ApplicationUser.Id == TestUser.Id);
        result.VisibleToRoles.Should().ContainSingle();
        (await _repository.GetById(Guid.NewGuid())).Should().BeNull();
    }

    [TestMethod]
    public async Task UpdateAndDelete_PersistChanges()
    {
        var quote = NewQuote("Hoi", new DateTime(2025, 1, 1));
        await _repository.Add(quote);

        quote.Text = "Aangepast";
        await _repository.Update(quote);
        (await _context.Quote.AsNoTracking().SingleAsync(TestContext.CancellationToken)).Text.Should().Be("Aangepast");

        await _repository.Delete(Guid.NewGuid());
        await _repository.Delete(quote.Id);
        (await _context.Quote.CountAsync(TestContext.CancellationToken)).Should().Be(0);
    }
}
