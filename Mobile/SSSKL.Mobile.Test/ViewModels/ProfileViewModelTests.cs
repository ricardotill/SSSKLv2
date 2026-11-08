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
}
