namespace SSSKLv2.Data.DAL;

// SQL-side GROUP BY result; long avoids DB-side sum overflow for very large totals
public record OrderAggregate(string UserId, long Amount);
