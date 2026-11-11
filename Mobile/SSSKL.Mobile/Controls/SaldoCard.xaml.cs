using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Controls;

/// <summary>The saldo card at the top of Kassa and Mijn saldo.</summary>
public partial class SaldoCard : ContentView
{
    public static readonly BindableProperty SaldoProperty =
        BindableProperty.Create(nameof(Saldo), typeof(string), typeof(SaldoCard), "", propertyChanged: (b, _, _) => ((SaldoCard)b).Update());

    public static readonly BindableProperty IsNegativeProperty =
        BindableProperty.Create(nameof(IsNegative), typeof(bool), typeof(SaldoCard), false, propertyChanged: (b, _, _) => ((SaldoCard)b).Update());

    public SaldoCard()
    {
        InitializeComponent();
    }

    public string Saldo { get => (string)GetValue(SaldoProperty); set => SetValue(SaldoProperty, value); }
    public bool IsNegative { get => (bool)GetValue(IsNegativeProperty); set => SetValue(IsNegativeProperty, value); }

    private void Update()
    {
        AmountLabel.Text = Saldo;
        Card.Background = (Brush)Resources[IsNegative ? "SaldoNegativeBrush" : "SaldoBrush"];
        SemanticProperties.SetDescription(Card, $"{Texts.MySaldo}: {Saldo}");
    }
}
