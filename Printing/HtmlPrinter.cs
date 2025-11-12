using System;
using System.Drawing.Printing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MenuBuPrinterAgent.Printing;

internal sealed class HtmlPrinter : IDisposable
{
    private WebView2? _webView;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;
    private bool _disposed;

    public string? SelectedPrinter { get; set; }
    public string PrinterWidth { get; set; } = "58mm";

    public async Task PrintHtmlAsync(string html, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_webView == null)
        {
            throw new InvalidOperationException("WebView2 başlatılamadı");
        }

        var tcs = new TaskCompletionSource<bool>();
        
        await _webView.CoreWebView2.ExecuteScriptAsync($@"
            document.open();
            document.write({System.Text.Json.JsonSerializer.Serialize(html)});
            document.close();
        ");

        // Sayfa yüklenmesini bekle
        await Task.Delay(500, cancellationToken);

        // Yazdırma ayarları
        var printSettings = _webView.CoreWebView2.Environment.CreatePrintSettings();
        printSettings.ShouldPrintBackgrounds = true;
        printSettings.ShouldPrintHeaderAndFooter = false;
        printSettings.MarginTop = 0;
        printSettings.MarginBottom = 0;
        printSettings.MarginLeft = 0;
        printSettings.MarginRight = 0;
        
        // Yazıcı seç
        if (!string.IsNullOrWhiteSpace(SelectedPrinter))
        {
            printSettings.PrinterName = SelectedPrinter;
        }

        try
        {
            var result = await _webView.CoreWebView2.PrintToPrinterAsync(printSettings);
            
            if (result != CoreWebView2PrintStatus.Succeeded)
            {
                throw new Exception($"Yazdırma başarısız: {result}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"HTML yazdırma hatası: {ex.Message}", ex);
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            _webView = new WebView2
            {
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "MenuBuPrinterAgent",
                        "WebView2"
                    )
                }
            };

            var env = await CoreWebView2Environment.CreateAsync(null, _webView.CreationProperties.UserDataFolder);
            await _webView.EnsureCoreWebView2Async(env);

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _webView?.Dispose();
        _initLock.Dispose();
    }
}
