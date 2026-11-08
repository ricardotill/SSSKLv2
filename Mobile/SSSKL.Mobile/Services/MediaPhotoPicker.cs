using Microsoft.Maui.Graphics.Platform;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Services;

/// <summary>Picks a photo and scales it down to a JPEG of at most 1024px, so uploads stay small.</summary>
public sealed class MediaPhotoPicker : IPhotoPicker
{
    private const float MaxDimension = 1024;

    public async Task<PickedPhoto?> PickAsync()
    {
        var files = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 });
        var file = files?.FirstOrDefault();
        if (file is null)
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        var image = PlatformImage.FromStream(stream);
        if (image.Width > MaxDimension || image.Height > MaxDimension)
        {
            image = image.Downsize(MaxDimension, disposeOriginal: true);
        }

        using var output = new MemoryStream();
        await image.SaveAsync(output, ImageFormat.Jpeg, quality: 0.85f);
        var name = Path.ChangeExtension(string.IsNullOrWhiteSpace(file.FileName) ? "profielfoto" : file.FileName, ".jpg");
        return new PickedPhoto(name, "image/jpeg", output.ToArray());
    }
}
