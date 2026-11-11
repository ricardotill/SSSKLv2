namespace SSSKL.Mobile.Controls;

/// <summary>
/// Shrinks a button slightly while it is held and springs it back on release. Uses the button's own
/// Pressed/Released events, so its native click handling and colours (incl. disabled) stay untouched.
/// </summary>
public sealed class PressScaleBehavior : Behavior<Button>
{
    public double PressedScale { get; set; } = 0.94;

    protected override void OnAttachedTo(Button bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.Pressed += OnPressed;
        bindable.Released += OnReleased;
    }

    protected override void OnDetachingFrom(Button bindable)
    {
        bindable.Pressed -= OnPressed;
        bindable.Released -= OnReleased;
        base.OnDetachingFrom(bindable);
    }

    private async void OnPressed(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.CancelAnimations();
            await button.ScaleToAsync(PressedScale, 80, Easing.CubicOut);
        }
    }

    private async void OnReleased(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.CancelAnimations();
            await button.ScaleToAsync(1, 220, Easing.SpringOut);
        }
    }
}
