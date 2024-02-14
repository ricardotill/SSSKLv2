// using FluentAssertions;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.VisualStudio.TestTools.UnitTesting;
// using SSSKLv2.Data;
// using SSSKLv2.Data.DAL;
// using SSSKLv2.Test.Util;
//
// namespace SSSKLv2.Test.Data.DAL;
//
// [TestClass]
// public class ApplicationUserRepositoryTests : RepositoryTest
// {
//     private MockDbContextFactory _dbContextFactory = null!;
//     private ApplicationUserRepository _sut = null!;
//
//     [TestInitialize]
//     public void TestInitialize()
//     {
//         InitializeDatabase();
//         _dbContextFactory = new MockDbContextFactory(GetOptions());
//         _sut = new ApplicationUserRepository(_dbContextFactory);
//     }
//     
//     [TestMethod]
//     public async Task GetAll_WhenApplicationUsersInDb_ReturnAll()
//     {
//         // Arrange
//         var a1 = new ApplicationUser
//         {
//             Id = Guid.NewGuid().ToString(),
//             UserName = "username1",
//             Name = "name1",
//             Surname = "surname1",
//             Email = "email1",
//             EmailConfirmed = true,
//             PhoneNumber = "phone1",
//             PhoneNumberConfirmed = true,
//             TwoFactorEnabled = true,
//             LockoutEnabled = true,
//             AccessFailedCount = 1,
//             Saldo = 20.95m
//         };
//         var a2 = new ApplicationUser
//         {
//             Id = Guid.NewGuid().ToString(),
//             UserName = "username2",
//             Name = "name1",
//             Surname = "surname1",
//             Email = "email2",
//             EmailConfirmed = true,
//             PhoneNumber = "phone2",
//             PhoneNumberConfirmed = true,
//             TwoFactorEnabled = true,
//             LockoutEnabled = true,
//             AccessFailedCount = 1,
//             Saldo = 10.05m
//         };
//         await SaveApplicationUsers(a1, a2);
//         
//         // Act
//         var result = await _sut.GetAll();
//
//         // Assert
//         var applicationUsers = result as ApplicationUser[] ?? result.ToArray();
//         applicationUsers.Should().HaveCount(2);
//         applicationUsers.Should().ContainEquivalentOf(a1);
//         applicationUsers.Should().ContainEquivalentOf(a2);
//     }
//     
//     private async Task SaveApplicationUsers(params object[] applicationUsers)
//     {
//         var hasher = new PasswordHasher<ApplicationUser>();
//         foreach (var applicationUser in applicationUsers)
//         {
//             if (applicationUser is ApplicationUser user)
//             {
//                 user.PasswordHash = hasher.HashPassword(null, "password");
//             }
//         }
//         await using var context = await _dbContextFactory.CreateDbContextAsync();
//         await context.Users.AddRangeAsync(applicationUsers);
//         await context.SaveChangesAsync();
//     }
//
//     [TestCleanup]
//     public void TestCleanup()
//     {
//         CleanupDatabase();
//     }
// }