using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class ProfileViewModelTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private readonly IPhotoPicker _photoPicker = Substitute.For<IPhotoPicker>();

    private ProfileViewModel CreateSut() => new(
        _api,
        new CurrentUserService(_api),
        _dialogs,
        _photoPicker,
        Substitute.For<IAppNavigator>(),
        new AppEndpoints(new Uri("https://api.test"), new Uri("https://web.test")),
        NullLogger<ProfileViewModel>.Instance);

    [TestMethod]
    public async Task ChangePictureCommand_PickerThrows_ShowsAlertInsteadOfCrashing()
    {
        // Arrange
        _photoPicker.PickAsync().ThrowsAsync(new NotSupportedException("no camera roll permission"));
        var sut = CreateSut();

        // Act
        var act = () => sut.ChangePictureCommand.ExecuteAsync(null);

        // Assert
        await act.Should().NotThrowAsync();
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.ProfilePictureFailed);
        await _api.DidNotReceiveWithAnyArgs().UploadProfilePictureAsync(default!);
    }

    [TestMethod]
    public async Task ChangePictureCommand_PickerCancelled_DoesNothing()
    {
        // Arrange
        _photoPicker.PickAsync().ThrowsAsync(new OperationCanceledException());
        var sut = CreateSut();

        // Act
        await sut.ChangePictureCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.DidNotReceiveWithAnyArgs().AlertAsync(default!, default!);
        await _api.DidNotReceiveWithAnyArgs().UploadProfilePictureAsync(default!);
    }

    [TestMethod]
    public async Task ChangePictureCommand_PickedPhoto_Uploads()
    {
        // Arrange
        _photoPicker.PickAsync().Returns(new PickedPhoto("foto.jpg", "image/jpeg", [1, 2, 3]));
        _api.GetMeAsync().Returns(new CurrentUser { Id = "1", FullName = "Ik Zelf", Roles = [RoleNames.User] });
        var sut = CreateSut();

        // Act
        await sut.ChangePictureCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).UploadProfilePictureAsync(Arg.Is<Base64FileUpload>(f => f.FileName == "foto.jpg" && f.Base64Content == "AQID"));
        await _dialogs.Received(1).ToastAsync(Texts.ProfilePictureUpdated);
    }

    [TestMethod]
    public void EditDescriptionCommand_FillsTheDraftAsPlainText()
    {
        // Arrange
        var sut = CreateSut();
        sut.DescriptionHtml = "<p>Hoi</p><p><br></p><p>Ik ben Jan</p>";

        // Act
        sut.EditDescriptionCommand.Execute(null);

        // Assert
        sut.IsEditingDescription.Should().BeTrue();
        sut.DescriptionDraft.Should().Be("Hoi\n\nIk ben Jan");
        sut.HasNoDescription.Should().BeFalse();
    }

    [TestMethod]
    public async Task SaveDescriptionCommand_SendsOnlyTheDescriptionAsHtml()
    {
        // Arrange
        _api.GetMeAsync().Returns(new CurrentUser { Id = "1", FullName = "Jan" });
        var sut = CreateSut();
        sut.EditDescriptionCommand.Execute(null);
        sut.DescriptionDraft = "Hoi & doei";

        // Act
        await sut.SaveDescriptionCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).UpdateMeAsync(new SelfUpdate(null, null, null, "<p>Hoi &amp; doei</p>"), Arg.Any<CancellationToken>());
        sut.DescriptionHtml.Should().Be("<p>Hoi &amp; doei</p>");
        sut.IsEditingDescription.Should().BeFalse();
        await _dialogs.Received(1).ToastAsync(Texts.DescriptionSaved);
    }

    [TestMethod]
    public async Task SaveDescriptionCommand_Cleared_SendsEmptyToRemoveIt()
    {
        // Arrange
        var sut = CreateSut();
        sut.DescriptionHtml = "<p>Oud</p>";
        sut.EditDescriptionCommand.Execute(null);
        sut.DescriptionDraft = "  ";

        // Act
        await sut.SaveDescriptionCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).UpdateMeAsync(new SelfUpdate(null, null, null, ""), Arg.Any<CancellationToken>());
        sut.DescriptionHtml.Should().BeNull();
        sut.HasNoDescription.Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveDescriptionCommand_Unchanged_KeepsTheFormattedHtml()
    {
        // Arrange
        var sut = CreateSut();
        sut.DescriptionHtml = "<p><strong>Vet</strong></p>";
        sut.EditDescriptionCommand.Execute(null);

        // Act
        await sut.SaveDescriptionCommand.ExecuteAsync(null);

        // Assert
        await _api.DidNotReceiveWithAnyArgs().UpdateMeAsync(default!, default);
        await _dialogs.DidNotReceiveWithAnyArgs().ConfirmAsync(default!, default!, default!, default!);
        sut.IsEditingDescription.Should().BeFalse();
        sut.DescriptionHtml.Should().Be("<p><strong>Vet</strong></p>");
    }

    [TestMethod]
    public async Task SaveDescriptionCommand_FormattingWouldBeLost_DeclinedKeepsEditing()
    {
        // Arrange
        _dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(false);
        var sut = CreateSut();
        sut.DescriptionHtml = "<p><strong>Vet</strong></p>";
        sut.EditDescriptionCommand.Execute(null);
        sut.DescriptionDraft = "Niet meer vet";

        // Act
        await sut.SaveDescriptionCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).ConfirmAsync(Texts.DescriptionLosesFormattingTitle, Texts.DescriptionLosesFormatting, Texts.SaveAnyway, Texts.Cancel);
        await _api.DidNotReceiveWithAnyArgs().UpdateMeAsync(default!, default);
        sut.IsEditingDescription.Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveDescriptionCommand_Fails_ShowsAlertAndKeepsTheDraft()
    {
        // Arrange
        _api.UpdateMeAsync(Arg.Any<SelfUpdate>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();
        sut.EditDescriptionCommand.Execute(null);
        sut.DescriptionDraft = "Nieuw";

        // Act
        await sut.SaveDescriptionCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.DescriptionSaveFailed);
        sut.IsEditingDescription.Should().BeTrue();
        sut.DescriptionDraft.Should().Be("Nieuw");
        sut.IsSavingDescription.Should().BeFalse();
    }
}
