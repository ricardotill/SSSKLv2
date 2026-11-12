namespace SSSKL.Mobile.Controls;

/// <summary>The type scale from <c>Styles.xaml</c> (<c>FontSize*</c>) for controls built in code.</summary>
internal static class TypeScale
{
    public static double Caption => Get("Caption");
    public static double Footnote => Get("Footnote");
    public static double Subhead => Get("Subhead");
    public static double Body => Get("Body");
    public static double Callout => Get("Callout");

    private static double Get(string name) => (double)Application.Current!.Resources["FontSize" + name];
}
