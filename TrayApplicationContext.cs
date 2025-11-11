using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MenuBuPrinterAgent.Printing;
using MenuBuPrinterAgent.Services;
using MenuBuPrinterAgent.UI;

namespace MenuBuPrinterAgent;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _statusMenuItem = new("Durum: Bağlanmadı") { Enabled = false };
    private readonly ToolStripMenuItem _loginMenuItem = new("&Giriş Yap");
    private readonly ToolStripMenuItem _logoutMenuItem = new("&Çıkış Yap") { Enabled = false };
    private readonly ToolStripMenuItem _reconnectMenuItem = new("&Yeniden Bağlan") { Enabled = false };
    private readonly ToolStripMenuItem _printerMenuItem = new("&Yazıcı Ayarla") { Enabled = false };
    private readonly ToolStripMenuItem _printerMappingMenuItem = new("Yazıcı Eşleştir") { Enabled = false };
    private readonly ToolStripMenuItem _clearQueueMenuItem = new("&Kuyruğu Temizle") { Enabled = false };
    private readonly ToolStripMenuItem _exitMenuItem = new("Çı&kış");

    private readonly UserSettings _settings = UserSettings.Load();
    private readonly HttpClient _httpClient = new();
    private readonly PrinterManager _printerManager;
    private MenuBuApiClient? _apiClient;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly SynchronizationContext _syncContext;
    private readonly SemaphoreSlim _pollLock = new(1, 1);
    private bool _initialPromptCompleted;
    private bool _isDisposed;
    private bool _isConnected;
    private readonly HashSet<int> _ignoredJobIds = new();
    private readonly HashSet<int> _processedJobIds = new();
    private readonly HashSet<int> _inFlightJobIds = new();
    private string? _lastConnectionError;

    public TrayApplicationContext()
    {
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        _printerManager = new PrinterManager(_httpClient)
        {
            SelectedPrinter = _settings.PrinterName
        };

        _pollTimer = new System.Windows.Forms.Timer { Interval = 6000 };
        _pollTimer.Tick += async (_, _) => await PollJobsAsync();

        _trayIcon = new NotifyIcon
        {
            Icon = ResourceHelper.GetTrayIcon(),
            Text = "MenuBu Yazıcı Ajanı",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        _loginMenuItem.Click += async (_, _) => await PromptLoginAsync();
        _logoutMenuItem.Click += (_, _) => Logout();
        _reconnectMenuItem.Click += async (_, _) => await ReconnectAsync();
        _printerMenuItem.Click += (_, _) => ShowPrinterDialog();
        _printerMappingMenuItem.Click += (_, _) => ShowPrinterMappingDialog();
        _clearQueueMenuItem.Click += async (_, _) => await ClearQueueAsync();
        _exitMenuItem.Click += (_, _) => ExitThread();

        _trayIcon.DoubleClick += (_, _) => ShowStatusBalloon();

        if (!string.IsNullOrWhiteSpace(_settings.Email) && !string.IsNullOrWhiteSpace(_settings.Password))
        {
            _ = AttemptAutoLoginAsync();
        }
        else
        {
            ShowStatus("Giriş yapın", connected: false);
        }
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(_statusMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_loginMenuItem);
        menu.Items.Add(_logoutMenuItem);
        menu.Items.Add(_reconnectMenuItem);
        menu.Items.Add(_printerMenuItem);
        menu.Items.Add(_printerMappingMenuItem);
        menu.Items.Add(_clearQueueMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_exitMenuItem);
        return menu;
    }

    private async Task AttemptAutoLoginAsync()
    {
        try
        {
            await AuthenticateAsync(_settings.Email, _settings.Password, silent: true);
        }
        catch
        {
            ShowStatus("Giriş gerekli", connected: false);
        }
    }

    private async Task PromptLoginAsync()
    {
        using var loginForm = new LoginForm(_settings.Email, _settings.Password);
        if (loginForm.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            await AuthenticateAsync(loginForm.Email, loginForm.Password, silent: false);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Giriş başarısız: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ShowStatus("Giriş yapılamadı", connected: false);
        }
    }

    private async Task AuthenticateAsync(string email, string password, bool silent)
    {
        CleanupClient();

        _apiClient = new MenuBuApiClient(email, password);
        IReadOnlyList<Models.PrintJob> initialJobs;
        try
        {
            initialJobs = await _apiClient.AuthenticateAndFetchInitialJobsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            CleanupClient();
            if (!silent)
            {
                MessageBox.Show($"Giriş sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            throw;
        }

        _settings.Email = email;
        _settings.Password = password;
        _settings.PrinterName = _printerManager.SelectedPrinter;
        _settings.PrinterWidth = _apiClient.PrinterWidth;
        _settings.Save();
        AutoStartManager.EnsureStartupEntry();
        _printerManager.PrinterWidth = _settings.PrinterWidth;
        _printerManager.FontSizeAdjustment = _settings.FontSizeAdjustment;

        _initialPromptCompleted = false;
        _ignoredJobIds.Clear();
        _processedJobIds.Clear();
        _inFlightJobIds.Clear();

        _loginMenuItem.Enabled = false;
        _logoutMenuItem.Enabled = true;
        _reconnectMenuItem.Enabled = true;
        _printerMenuItem.Enabled = true;
        _printerMappingMenuItem.Enabled = true;
        _clearQueueMenuItem.Enabled = true;

        ShowStatus($"Bağlandı: {_apiClient.BusinessName}", connected: true);
        _syncContext.Post(_ => 
            _trayIcon.ShowBalloonTip(2000, "MenuBu", $"{_apiClient.BusinessName} hesabına bağlanıldı.", ToolTipIcon.Info), null);

        EnsureTimer();
        await HandleInitialJobsAsync(initialJobs);
    }

    private void EnsureTimer()
    {
        if (!_pollTimer.Enabled)
        {
            _pollTimer.Start();
        }

        _ = PollJobsAsync();
    }

    private async Task HandleInitialJobsAsync(IReadOnlyList<Models.PrintJob> jobs)
    {
        if (jobs.Count == 0)
        {
            _initialPromptCompleted = true;
            return;
        }

        var result = MessageBox.Show(
            $"{jobs.Count} adet bekleyen yazdırma bulunuyor. Hepsini yazdırmak ister misiniz?",
            "Bekleyen Yazdırmalar",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            foreach (var job in jobs.OrderBy(j => j.CreatedAt))
            {
                await ProcessJobAsync(job);
            }
        }
        else
        {
            foreach (var job in jobs)
            {
                _ignoredJobIds.Add(job.Id);
            }
        }

        _initialPromptCompleted = true;
    }

    private async Task PollJobsAsync()
    {
        if (_apiClient == null)
        {
            return;
        }

        if (!await _pollLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            IReadOnlyList<Models.PrintJob> jobs;
            try
            {
                jobs = await _apiClient.GetPendingJobsAsync(CancellationToken.None);
                if (!_isConnected)
                {
                    ShowStatus($"Bağlandı: {_apiClient.BusinessName}", connected: true);
                    NotifyConnectionRestored();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Bağlantı hatası: {ex.Message}", connected: false);
                NotifyConnectionLost(ex.Message);
                return;
            }

            if (!_initialPromptCompleted)
            {
                await HandleInitialJobsAsync(jobs);
                return;
            }

            foreach (var job in jobs.OrderBy(j => j.CreatedAt))
            {
                if (_ignoredJobIds.Contains(job.Id) || _processedJobIds.Contains(job.Id) || _inFlightJobIds.Contains(job.Id))
                {
                    continue;
                }

                await ProcessJobAsync(job);
            }
        }
        finally
        {
            _pollLock.Release();
        }
    }

    private async Task ProcessJobAsync(Models.PrintJob job)
    {
        if (_apiClient == null)
        {
            return;
        }

        if (!_inFlightJobIds.Add(job.Id))
        {
            return;
        }

        try
        {
            await SafeUpdateJobStatus(job.Id, "printing", null);
            
            var targetPrinter = SelectPrinterForJob(job);
            var previousPrinter = _printerManager.SelectedPrinter;
            
            if (targetPrinter != null)
            {
                _printerManager.SelectedPrinter = targetPrinter;
            }
            
            try
            {
                using var payloadDoc = JsonDocument.Parse(job.Payload.ToJsonString());
                await _printerManager.PrintAsync(payloadDoc, CancellationToken.None);
            }
            finally
            {
                _printerManager.SelectedPrinter = previousPrinter;
            }
            
            await SafeUpdateJobStatus(job.Id, "printed", null);
            _processedJobIds.Add(job.Id);
        }
        catch (Exception ex)
        {
            var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMsg += $" | Inner: {ex.InnerException.Message}";
            }
            await SafeUpdateJobStatus(job.Id, "failed", errorMsg);
            _syncContext.Post(_ => 
                _trayIcon.ShowBalloonTip(5000, "MenuBu Yazıcı", $"#{job.Id} yazdırılamadı: {errorMsg}", ToolTipIcon.Warning), null);
        }
        finally
        {
            _inFlightJobIds.Remove(job.Id);
        }
    }

    private async Task SafeUpdateJobStatus(int jobId, string status, string? message)
    {
        if (_apiClient == null)
        {
            return;
        }

        try
        {
            await _apiClient.UpdateJobStatusAsync(jobId, status, message, CancellationToken.None);
        }
        catch
        {
            // ignore
        }
    }

    private async Task ReconnectAsync()
    {
        if (_apiClient == null)
        {
            await PromptLoginAsync();
            return;
        }

        ShowStatus("Yeniden bağlanıyor...", connected: false);
        try
        {
            await AuthenticateAsync(_settings.Email, _settings.Password, silent: false);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yeniden bağlanma başarısız: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowPrinterDialog()
    {
        using var form = new PrinterSettingsForm(_printerManager.SelectedPrinter, _settings.PrinterWidth, _settings.FontSizeAdjustment);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _printerManager.SelectedPrinter = form.SelectedPrinter;
            _printerManager.PrinterWidth = form.PrinterWidth;
            _printerManager.FontSizeAdjustment = form.FontSizeAdjustment;
            _settings.PrinterName = form.SelectedPrinter;
            _settings.PrinterWidth = form.PrinterWidth;
            _settings.FontSizeAdjustment = form.FontSizeAdjustment;
            _settings.Save();
            var message = form.SelectedPrinter is null ? "Varsayılan yazıcı kullanılacak." : $"Yazıcı seçildi: {form.SelectedPrinter}";
            _syncContext.Post(_ => 
                _trayIcon.ShowBalloonTip(2000, "MenuBu Yazıcı", message, ToolTipIcon.Info), null);
        }
    }

    private void ShowPrinterMappingDialog()
    {
        if (_apiClient == null)
        {
            MessageBox.Show("Önce giriş yapmalısınız.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var printerConfigs = _apiClient.PrinterConfigs;
        if (printerConfigs.Count == 0)
        {
            MessageBox.Show("Siteden henüz yazıcı tanımlanmamış. Lütfen önce web panelinden yazıcı ekleyin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new PrinterMappingForm(printerConfigs, _settings.PrinterMappings);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings.PrinterMappings = form.PrinterMappings;
            _settings.Save();
            var count = form.PrinterMappings.Count;
            var message = count > 0 ? $"{count} yazıcı eşleştirildi." : "Eşleştirmeler temizlendi.";
            _syncContext.Post(_ => 
                _trayIcon.ShowBalloonTip(2000, "MenuBu Yazıcı", message, ToolTipIcon.Info), null);
        }
    }

    private void Logout()
    {
        CleanupClient();
        _settings.Email = string.Empty;
        _settings.Password = string.Empty;
        _settings.Save();
        ShowStatus("Çıkış yapıldı", connected: false);
        _syncContext.Post(_ => 
            _trayIcon.ShowBalloonTip(2000, "MenuBu Yazıcı", "Hesaptan çıkış yapıldı.", ToolTipIcon.Info), null);
        _loginMenuItem.Enabled = true;
        _logoutMenuItem.Enabled = false;
        _reconnectMenuItem.Enabled = false;
        _printerMenuItem.Enabled = false;
        _printerMappingMenuItem.Enabled = false;
        _clearQueueMenuItem.Enabled = false;
    }

    private void ShowStatus(string message, bool connected)
    {
        void Update()
        {
            _isConnected = connected;
            _statusMenuItem.Text = $"Durum: {message}";
            _trayIcon.Text = $"MenuBu Yazıcı Ajanı - {message}".Trim();
        }

        if (SynchronizationContext.Current == _syncContext)
        {
            Update();
        }
        else
        {
            _syncContext.Post(_ => Update(), null);
        }
    }

    private void ShowStatusBalloon()
    {
        void Show()
        {
            _trayIcon.ShowBalloonTip(2000, "MenuBu Yazıcı", _statusMenuItem.Text, _isConnected ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }

        if (SynchronizationContext.Current == _syncContext)
        {
            Show();
        }
        else
        {
            _syncContext.Post(_ => Show(), null);
        }
    }

    private void CleanupClient()
    {
        if (_pollTimer.Enabled)
        {
            _pollTimer.Stop();
        }
        _pollLock.Reset();
        _apiClient?.Dispose();
        _apiClient = null;
        _initialPromptCompleted = false;
        _ignoredJobIds.Clear();
        _processedJobIds.Clear();
        _inFlightJobIds.Clear();
        _lastConnectionError = null;
    }

    private async Task ClearQueueAsync()
    {
        if (_apiClient == null)
        {
            await PromptLoginAsync();
            return;
        }

        if (MessageBox.Show("Bekleyen yazdırma kuyruğunu temizlemek istediğinizden emin misiniz?\nBu işlem yalnızca bu işletmeye ait bekleyen işleri temizler.", "Kuyruğu Temizle", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _pollTimer.Stop();
        ShowStatus("Kuyruk temizleniyor...", _isConnected);

        try
        {
            var cleared = await _apiClient.ClearPendingJobsAsync(CancellationToken.None);
            _ignoredJobIds.Clear();
            _processedJobIds.Clear();
            _inFlightJobIds.Clear();

            var message = cleared > 0 ? $"{cleared} bekleyen iş temizlendi." : "Bekleyen iş bulunamadı.";
            _syncContext.Post(_ => 
                _trayIcon.ShowBalloonTip(2000, "MenuBu Yazıcı", message, ToolTipIcon.Info), null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kuyruk temizlenemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            EnsureTimer();
        }
    }

    protected override void ExitThreadCore()
    {
        if (_isDisposed)
        {
            base.ExitThreadCore();
            return;
        }

        _isDisposed = true;
        CleanupClient();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _httpClient.Dispose();
        base.ExitThreadCore();
    }

    private void NotifyConnectionLost(string message)
    {
        if (_lastConnectionError == message)
        {
            return;
        }

        _lastConnectionError = message;
        var tipText = $"Bağlantı sorunu: {message ?? "Bilinmeyen hata"}";
        _syncContext.Post(_ =>
            _trayIcon.ShowBalloonTip(3000, "MenuBu Yazıcı", tipText, ToolTipIcon.Warning), null);
    }

    private void NotifyConnectionRestored()
    {
        if (_lastConnectionError == null)
        {
            return;
        }

        _lastConnectionError = null;
        _syncContext.Post(_ =>
            _trayIcon.ShowBalloonTip(2000, "MenuBu Yazıcı", "Bağlantı yeniden kuruldu.", ToolTipIcon.Info), null);
    }

    private string? SelectPrinterForJob(Models.PrintJob job)
    {
        if (_apiClient == null || _apiClient.PrinterConfigs.Count == 0)
        {
            return null; // Varsayılan yazıcı kullanılacak
        }

        var jobType = job.JobType?.ToLowerInvariant() ?? "receipt";
        
        // Önce default yazıcıyı bul
        foreach (var config in _apiClient.PrinterConfigs)
        {
            if (!config.IsActive)
            {
                continue;
            }
            
            var printerType = config.PrinterType.ToLowerInvariant();
            if (printerType == "all" || printerType == jobType)
            {
                if (config.IsDefault)
                {
                    // Eşleştirme var mı kontrol et
                    if (_settings.PrinterMappings.TryGetValue(config.Name, out var mappedPrinter))
                    {
                        return mappedPrinter;
                    }
                    // Eşleştirme yoksa null dön (varsayılan kullanılacak)
                    return null;
                }
            }
        }
        
        // Default yoksa ilk uygun yazıcıyı bul
        foreach (var config in _apiClient.PrinterConfigs)
        {
            if (!config.IsActive)
            {
                continue;
            }
            
            var printerType = config.PrinterType.ToLowerInvariant();
            if (printerType == "all" || printerType == jobType)
            {
                // Eşleştirme var mı kontrol et
                if (_settings.PrinterMappings.TryGetValue(config.Name, out var mappedPrinter))
                {
                    return mappedPrinter;
                }
                // Eşleştirme yoksa null dön (varsayılan kullanılacak)
                return null;
            }
        }
        
        return null; // Varsayılan yazıcı kullanılacak
    }
}

internal static class SemaphoreExtensions
{
    public static void Reset(this SemaphoreSlim semaphore)
    {
        try
        {
            if (semaphore.Wait(0))
            {
                semaphore.Release();
            }
        }
        catch
        {
            // ignore
        }
    }
}
