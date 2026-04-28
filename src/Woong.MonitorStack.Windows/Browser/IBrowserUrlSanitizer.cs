namespace Woong.MonitorStack.Windows.Browser;

public interface IBrowserUrlSanitizer
{
    BrowserActivitySnapshot Sanitize(BrowserActivitySnapshot snapshot, BrowserUrlStoragePolicy storagePolicy);
}
