using CommunityToolkit.Mvvm.ComponentModel;
using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core.ViewModels;

public sealed partial class ProductItemViewModel(OrderProduct product) : ObservableObject
{
    public Guid Id => product.Id;
    public string Name => product.Name;
    public decimal Price => product.Price;
    public int Stock => product.Stock;
    public bool IsOutOfStock => product.Stock <= 0;

    /// <summary>The tile's monogram: the first letter or digit of the name, upper case.</summary>
    public string Initial { get; } = InitialOf(product.Name);

    public string PriceText => Display.Money(product.Price);

    public string Description => $"{product.Name}, {Display.Money(product.Price)}";

    internal static string InitialOf(string name)
    {
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c))
                return char.ToUpper(c, Display.Dutch).ToString();
        }
        return "?";
    }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
