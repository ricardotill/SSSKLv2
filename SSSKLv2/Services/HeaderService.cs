using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class HeaderService : IHeaderService
{
    public string Saldo { get; set; } = "";
    public string Name { get; set; } = "";

    public event EventHandler? HeaderChanged;

    public void SetName(string name)
    {
        Name = name;
        HeaderChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void SetSaldo(string saldo)
    {
        Saldo = saldo;
        HeaderChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifyHeaderChanged()
        => HeaderChanged?.Invoke(this, EventArgs.Empty);
}