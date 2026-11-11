using System.Windows.Input;

namespace SSSKL.Mobile.Controls;

/// <summary>A quote card (list and detail). The vote is its own tap target inside the card.</summary>
public partial class QuoteCard : ContentView
{
    public static readonly BindableProperty VoteCommandProperty =
        BindableProperty.Create(nameof(VoteCommand), typeof(ICommand), typeof(QuoteCard), null,
            propertyChanged: (b, _, n) => ((QuoteCard)b).VoteButton.Command = (ICommand?)n);

    public static readonly BindableProperty OpenCommandProperty =
        BindableProperty.Create(nameof(OpenCommand), typeof(ICommand), typeof(QuoteCard), null,
            propertyChanged: (b, _, n) => ((QuoteCard)b).OpenArea.Command = (ICommand?)n);

    /// <summary>0 shows the whole text (detail).</summary>
    public static readonly BindableProperty MaxTextLinesProperty =
        BindableProperty.Create(nameof(MaxTextLines), typeof(int), typeof(QuoteCard), 0,
            propertyChanged: (b, _, n) => ((QuoteCard)b).ApplyMaxLines((int)n));

    public QuoteCard()
    {
        InitializeComponent();
        ApplyMaxLines(0);
    }

    public ICommand? VoteCommand { get => (ICommand?)GetValue(VoteCommandProperty); set => SetValue(VoteCommandProperty, value); }
    /// <summary>Null on the detail page: the text isn't a link there.</summary>
    public ICommand? OpenCommand { get => (ICommand?)GetValue(OpenCommandProperty); set => SetValue(OpenCommandProperty, value); }
    public int MaxTextLines { get => (int)GetValue(MaxTextLinesProperty); set => SetValue(MaxTextLinesProperty, value); }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        VoteButton.CommandParameter = BindingContext;
        OpenArea.CommandParameter = BindingContext;
        if (BindingContext is Core.ViewModels.QuoteRow row)
        {
            SemanticProperties.SetDescription(OpenArea, row.AccessibleText);
        }
    }

    private void ApplyMaxLines(int lines)
    {
        TextLabel.MaxLines = lines > 0 ? lines : -1;
        TextLabel.LineBreakMode = lines > 0 ? LineBreakMode.TailTruncation : LineBreakMode.WordWrap;
    }
}
