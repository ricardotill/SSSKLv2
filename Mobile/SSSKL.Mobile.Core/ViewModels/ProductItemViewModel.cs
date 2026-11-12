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

    public string PriceText => Display.Money(product.Price);

    public string Description => $"{product.Name}, {Display.Money(product.Price)}";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
