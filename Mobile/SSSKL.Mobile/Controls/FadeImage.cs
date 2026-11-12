namespace SSSKL.Mobile.Controls;

/// <summary>
/// An image that fades in once it has loaded instead of popping in; until then whatever is behind it (initials,
/// a placeholder colour) shows. Remote images are cached by MAUI's <see cref="UriImageSource"/>.
/// </summary>
public sealed class FadeImage : Image
{
    /// <summary>If a platform never reports loading, show the image anyway after this long.</summary>
    private static readonly TimeSpan Fallback = TimeSpan.FromSeconds(2);

    private int _sourceVersion;

    public FadeImage()
    {
        Opacity = 0;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == SourceProperty.PropertyName)
        {
            var version = ++_sourceVersion;
            this.CancelAnimations();
            Opacity = 0;
            if (Source is not null)
            {
                Dispatcher.DispatchDelayed(Fallback, () =>
                {
                    if (version == _sourceVersion && Opacity == 0)
                    {
                        Opacity = 1;
                    }
                });
            }
        }
        else if (propertyName == IsLoadingProperty.PropertyName && !IsLoading && Source is not null && Opacity < 1)
        {
            _ = this.FadeToAsync(1, 220, Easing.CubicOut);
        }
    }
}
