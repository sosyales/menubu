using System;
using System.Runtime.InteropServices;
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
    private CoreWebView2Environment? _environment;

    public string? SelectedPrinter { get; set; }
    public string PrinterWidth { get; set; } = "58mm";

    public async Task PrintHtmlAsync(string html, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_webView == null)
        {
            throw new InvalidOperationException("WebView2 başlatılamadı");
        }

        await LoadHtmlAsync(html, cancellationToken);

        try
        {
            await PrintWithCoreAsync(_webView.CoreWebView2, cancellationToken);
        }
        catch (NotImplementedException)
        {
            await FallbackWindowPrintAsync(html, cancellationToken);
        }
        catch (COMException ex) when ((uint)ex.HResult == 0x80004001) // E_NOTIMPL
        {
            await FallbackWindowPrintAsync(html, cancellationToken);
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MenuBuPrinterAgent",
                "WebView2");

            _webView = new WebView2
            {
                CreationProperties = new CoreWebView2CreationProperties
                {
                    UserDataFolder = userDataFolder
                }
            };

            _environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await _webView.EnsureCoreWebView2Async(_environment);

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task LoadHtmlAsync(string html, CancellationToken cancellationToken)
    {
        if (_webView == null)
        {
            throw new InvalidOperationException("WebView2 mevcut değil");
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Handler(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess)
            {
                tcs.TrySetException(new InvalidOperationException($"HTML yüklenemedi: {args.WebErrorStatus}"));
            }
            else
            {
                tcs.TrySetResult(true);
            }
        }

        _webView.NavigationCompleted += Handler;
        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        try
        {
            _webView.NavigateToString(html);
            await tcs.Task;
        }
        finally
        {
            _webView.NavigationCompleted -= Handler;
        }
    }

    private async Task PrintWithCoreAsync(CoreWebView2 coreWebView2, CancellationToken cancellationToken)
    {
        if (_environment == null)
        {
            throw new InvalidOperationException("WebView2 ortamı oluşturulamadı");
        }

        var settings = _environment.CreatePrintSettings();
        settings.ShouldPrintHeaderAndFooter = false;
        settings.ShouldPrintBackgrounds = true;
        settings.ScaleFactor = GetScaleFactor();
        if (!string.IsNullOrWhiteSpace(SelectedPrinter))
        {
            settings.PrinterName = SelectedPrinter;
        }

        var status = await coreWebView2.PrintAsync(CoreWebView2PrintTarget.Printer, settings);
        if (status != CoreWebView2PrintStatus.Succeeded)
        {
            throw new InvalidOperationException($"Yazdırma başarısız: {status}");
        }
    }

    private async Task FallbackWindowPrintAsync(string html, CancellationToken cancellationToken)
    {
        if (_webView?.CoreWebView2 == null)
        {
            throw new InvalidOperationException("WebView2 hazır değil");
        }

        await _webView.CoreWebView2.ExecuteScriptAsync($@"
            document.open();
            document.write({System.Text.Json.JsonSerializer.Serialize(html)});
            document.close();
        ");

        await Task.Delay(500, cancellationToken);
        await _webView.CoreWebView2.ExecuteScriptAsync("window.print();");
        await Task.Delay(1000, cancellationToken);
    }

    private double GetScaleFactor() =>
        PrinterWidth.StartsWith("80", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.78;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _webView?.Dispose();
        _initLock.Dispose();
        _environment = null;
    }
}
