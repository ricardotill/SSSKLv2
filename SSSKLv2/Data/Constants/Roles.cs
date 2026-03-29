namespace SSSKLv2.Data.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Kiosk = "Kiosk";
    public const string Guest = "Guest";

    public static readonly string[] AllProtected = { Admin, User, Kiosk, Guest };
}
