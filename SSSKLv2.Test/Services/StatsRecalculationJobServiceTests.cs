using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class StatsRecalculationJobServiceTests
{
    [TestMethod]
    public async Task StartRecalculateAll_OnlyStartsOneActiveJobAndCompletesSuccessfully()
    {
        var usersReady = new TaskCompletionSource<IList<ApplicationUser>>();
        var userRepository = Substitute.For<IApplicationUserRepository>();
        userRepository.GetAll().Returns(usersReady.Task);
        var stats = Substitute.For<IUserStatRepository>();
        var notifications = Substitute.For<INotificationService>();
        var services = CreateScopeFactory(userRepository, stats, notifications);
        var sut = CreateSut(services);

        var first = sut.StartRecalculateAll("admin");
        var second = sut.StartRecalculateAll("other-admin");

        first.Started.Should().BeTrue();
        second.Started.Should().BeFalse();
        second.Job.Id.Should().Be(first.Job.Id);

        usersReady.SetResult(new List<ApplicationUser>
        {
            new() { Id = "user-1" },
            new() { Id = "user-2" }
        });
        var completed = await WaitForTerminalState(sut, first.Job.Id);

        completed!.Status.Should().Be(RecalculationJobStatus.Completed);
        completed.TotalUsers.Should().Be(2);
        completed.ProcessedUsers.Should().Be(2);
        completed.FailedUsers.Should().Be(0);
        await notifications.Received(1).CreateNotificationAsync("admin", Arg.Any<string>(), Arg.Any<string>(), sendPush: true);
        await stats.Received(1).RecalculateByUserId("user-1");
        await stats.Received(1).RecalculateByUserId("user-2");
    }

    [TestMethod]
    public async Task RunJob_RecordsFailedUsersAndStillCompletes()
    {
        var userRepository = Substitute.For<IApplicationUserRepository>();
        userRepository.GetAll().Returns(new List<ApplicationUser>
        {
            new() { Id = "good" }, new() { Id = "bad" }
        });
        var stats = Substitute.For<IUserStatRepository>();
        stats.RecalculateByUserId("bad").Returns<Task<UserStat>>(_ => throw new InvalidOperationException("bad user"));
        var notifications = Substitute.For<INotificationService>();
        var sut = CreateSut(CreateScopeFactory(userRepository, stats, notifications));

        var started = sut.StartRecalculateAll("admin");
        var completed = await WaitForTerminalState(sut, started.Job.Id);

        completed!.Status.Should().Be(RecalculationJobStatus.Completed);
        completed.ProcessedUsers.Should().Be(2);
        completed.FailedUsers.Should().Be(1);
        completed.CompletedAt.Should().NotBeNull();
    }

    [TestMethod]
    public async Task RunJob_WhenLoadingUsersFails_MarksJobFailedAndNotifiesStarter()
    {
        var userRepository = Substitute.For<IApplicationUserRepository>();
        userRepository.GetAll().Returns<Task<IList<ApplicationUser>>>(_ => throw new InvalidOperationException("database down"));
        var notifications = Substitute.For<INotificationService>();
        var sut = CreateSut(CreateScopeFactory(userRepository, Substitute.For<IUserStatRepository>(), notifications));

        var started = sut.StartRecalculateAll("admin");
        var failed = await WaitForTerminalState(sut, started.Job.Id);

        failed!.Status.Should().Be(RecalculationJobStatus.Failed);
        failed.ErrorMessage.Should().Be("database down");
        await notifications.Received(1).CreateNotificationAsync("admin", Arg.Any<string>(), Arg.Is<string>(message => message.Contains("database down")), sendPush: true);
    }

    private static StatsRecalculationJobService CreateSut(IServiceScopeFactory scopeFactory)
        => new(scopeFactory, Substitute.For<ILogger<StatsRecalculationJobService>>());

    private static IServiceScopeFactory CreateScopeFactory(
        IApplicationUserRepository userRepository,
        IUserStatRepository stats,
        INotificationService notifications)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IApplicationUserRepository)).Returns(userRepository);
        provider.GetService(typeof(IUserStatRepository)).Returns(stats);
        provider.GetService(typeof(INotificationService)).Returns(notifications);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        var factory = Substitute.For<IServiceScopeFactory>();
        factory.CreateScope().Returns(scope);
        return factory;
    }

    private static async Task<RecalculationJobDto?> WaitForTerminalState(StatsRecalculationJobService sut, Guid jobId)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var status = sut.GetStatus(jobId);
            if (status?.Status is RecalculationJobStatus.Completed or RecalculationJobStatus.Failed)
                return status;
            await Task.Delay(10);
        }

        return sut.GetStatus(jobId);
    }
}