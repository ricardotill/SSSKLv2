using SSSKL.Mobile.Views;

namespace SSSKL.Mobile;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        // Pages come from DI; DataTemplates resolve them lazily on first visit.
        PosContent.ContentTemplate = new DataTemplate(services.GetRequiredService<PosPage>);
        ProfileContent.ContentTemplate = new DataTemplate(services.GetRequiredService<ProfilePage>);
        MoreContent.ContentTemplate = new DataTemplate(services.GetRequiredService<WebPage>);
    }
}
