using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Controls;

/// <summary>One option of a <see cref="SegmentedControl"/>: its text, an optional Tabler icon and the value it stands for.</summary>
public sealed class SegmentItem
{
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public object? Value { get; set; }
}

/// <summary>
/// A segmented control (the leaderboard period, the colour mode): a sunken track with one segment per
/// <see cref="SegmentItem"/> and a raised pill that slides to the selected one. <see cref="SelectedValue"/>
/// comes from the view model; a tap runs <see cref="Command"/> with the segment's value, so the view model
/// stays the one that decides.
/// </summary>
[ContentProperty(nameof(Items))]
public sealed class SegmentedControl : ContentView
{
    private const double Spacing = 3;

    public static readonly BindableProperty SelectedValueProperty =
        BindableProperty.Create(nameof(SelectedValue), typeof(object), typeof(SegmentedControl), null,
            propertyChanged: (b, _, _) => ((SegmentedControl)b).UpdateSelection(animate: true));

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SegmentedControl));

    private readonly Grid _grid = new() { ColumnSpacing = Spacing };
    private readonly Border _pill;
    private readonly List<(Grid Cell, Label Label, IconView? Icon)> _segments = [];
    private int _selected = -1;

    public SegmentedControl()
    {
        var items = new ObservableCollection<SegmentItem>();
        items.CollectionChanged += OnItemsChanged;
        Items = items;

        _pill = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 9 },
            StrokeThickness = 0,
            InputTransparent = true,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.08f, Radius = 4, Offset = new Point(0, 1) },
            IsVisible = false
        };
        _pill.SetAppThemeColor(BackgroundColorProperty, Resource("SurfaceLight"), Resource("SurfaceDark"));
        _grid.Add(_pill);
        _grid.SizeChanged += (_, _) => UpdateSelection(animate: false);

        var track = new Border { Style = (Style)Application.Current!.Resources["SegmentTrack"], Content = _grid };
        Content = track;
    }

    public IList<SegmentItem> Items { get; }
    public object? SelectedValue { get => GetValue(SelectedValueProperty); set => SetValue(SelectedValueProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var (cell, _, _) in _segments)
            _grid.Remove(cell);
        _segments.Clear();
        _grid.ColumnDefinitions.Clear();

        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var label = new Label { Text = item.Text, Style = (Style)Application.Current!.Resources["SegmentText"] };
            var row = new HorizontalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            IconView? icon = null;
            if (!string.IsNullOrEmpty(item.Icon))
            {
                icon = new IconView { Icon = item.Icon, Size = 18 };
                row.Add(icon);
            }
            row.Add(label);

            var cell = new Grid { Padding = new Thickness(8, 10), MinimumHeightRequest = 44, Children = { row } };
            SemanticProperties.SetDescription(cell, item.Text);
            var index = i;
            cell.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnTapped(index)) });
            _grid.Add(cell, i);
            _segments.Add((cell, label, icon));
        }
        UpdateSelection(animate: false);
    }

    private void OnTapped(int index)
    {
        var value = Items[index].Value;
        if (Equals(value, SelectedValue))
            return;
        Handler?.MauiContext?.Services.GetService<IHaptics>()?.Selection();
        if (Command?.CanExecute(value) == true)
            Command.Execute(value);
    }

    private void UpdateSelection(bool animate)
    {
        var selected = -1;
        for (var i = 0; i < Items.Count; i++)
        {
            if (Equals(Items[i].Value, SelectedValue))
                selected = i;
        }

        for (var i = 0; i < _segments.Count; i++)
        {
            var (cell, label, icon) = _segments[i];
            var isSelected = i == selected;
            label.FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None;
            label.SetAppThemeColor(Label.TextColorProperty,
                Resource(isSelected ? "HeadingLight" : "MutedLight"), Resource(isSelected ? "HeadingDark" : "MutedDark"));
            icon?.SetAppThemeColor(IconView.ColorProperty,
                Resource(isSelected ? "Primary600" : "MutedLight"), Resource(isSelected ? "Primary400" : "MutedDark"));
            SemanticProperties.SetHint(cell, isSelected ? Core.Localization.Texts.Selected : null);
        }

        _pill.IsVisible = selected >= 0;
        if (selected < 0 || _grid.Width <= 0 || _segments.Count == 0)
        {
            _selected = selected;
            return;
        }

        var segmentWidth = (_grid.Width - Spacing * (_segments.Count - 1)) / _segments.Count;
        var x = selected * (segmentWidth + Spacing);
        _pill.CancelAnimations();
        if (animate && _selected >= 0 && _selected != selected)
            _ = _pill.TranslateToAsync(x, 0, 220, Easing.CubicOut);
        else
            _pill.TranslationX = x;
        _selected = selected;
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
