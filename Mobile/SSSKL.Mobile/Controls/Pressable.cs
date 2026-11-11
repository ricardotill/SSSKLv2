using System.Windows.Input;
using CommunityToolkit.Maui.Behaviors;

namespace SSSKL.Mobile.Controls;

/// <summary>
/// A transparent tap target that shrinks its content slightly while pressed. Wrap a themed <c>Border</c> in
/// it rather than putting a <see cref="TouchBehavior"/> on the border itself: TouchBehavior snapshots the
/// background of the element it's attached to and restores it on every state change, which would override
/// theme and selection colours.
/// </summary>
public sealed class Pressable : ContentView
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(Pressable), null, propertyChanged: (b, _, n) => ((Pressable)b)._touch.Command = (ICommand?)n);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(Pressable), null, propertyChanged: (b, _, n) => ((Pressable)b)._touch.CommandParameter = n);

    private readonly TouchBehavior _touch = new()
    {
        PressedScale = 0.96,
        DefaultAnimationDuration = 160,
        DefaultAnimationEasing = Easing.CubicOut,
        PressedAnimationDuration = 80,
        PressedAnimationEasing = Easing.CubicOut
    };

    public Pressable()
    {
        BackgroundColor = Colors.Transparent;
        Behaviors.Add(_touch);
    }

    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
}
