using Foundation;
using WebKit;

namespace SSSKL.Mobile.Services;

public sealed partial class WebDataCleaner
{
    private partial Task ClearPlatformAsync() =>
        WKWebsiteDataStore.DefaultDataStore.RemoveDataOfTypesAsync(WKWebsiteDataStore.AllWebsiteDataTypes, NSDate.DistantPast);
}
