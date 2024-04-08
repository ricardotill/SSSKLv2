namespace SSSKLv2.Services.Interfaces;

public interface IHeaderService
{
    public string Saldo { get; set; }
    public bool SaldoNegative => Saldo.Contains("-");
    public string Name { get; set; }
    public event EventHandler? HeaderChanged;
    public void SetName(string name);
    public void SetSaldo(string saldo);
    public void NotifyHeaderChanged();
}