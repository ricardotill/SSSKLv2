using System.Globalization;
using System.Text;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Orders;

public static class OrderCsvWriter
{
    public static string Write(IEnumerable<Order> orders)
    {
        var csv = new StringBuilder();

        // Header
        csv.AppendLine("OrderId,CustomerUsername,OrderDateTime,ProductName,ProductAmount,TotalPaid");

        // Rows
        foreach (var order in orders)
        {
            csv.AppendLine(
                $"{order.Id},{EscapeCsvField(order.User?.UserName)},{order.CreatedOn:yyyy-MM-dd HH:mm:ss},{EscapeCsvField(order.ProductNaam)},{order.Amount},{order.Paid.ToString(CultureInfo.InvariantCulture)}");
        }

        return csv.ToString();
    }

    // Escapes a field for CSV output, including formula injection mitigation
    private static string EscapeCsvField(string? field)
    {
        if (field == null)
            return "\"\"";
        // Formula injection mitigation: prefix with ' if starts with =, +, -, or @
        if (field.StartsWith('=') || field.StartsWith('+') || field.StartsWith('-') || field.StartsWith('@'))
            field = "'" + field;
        // Escape double quotes by doubling them
        field = field.Replace("\"", "\"\"");
        // Wrap in double quotes
        return $"\"{field}\"";
    }
}
