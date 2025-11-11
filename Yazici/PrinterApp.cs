using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MenuBuPrinter;

public class PrinterApp : ApplicationContext
{
    private NotifyIcon? _trayIcon;
    private readonly HttpClient _http = new();
    private System.Windows.Forms.Timer? _pollTimer;
    private readonly HashSet<int> _processed = new();
    private readonly SynchronizationContext _syncContext;
    
    private string _email = "";
    private string _password = "";
    private string _printerName = "";
    private int _businessId;
    private bool _connected;

    public PrinterApp()
    {
        _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        LoadSettings();
        InitializeTrayIcon();
        
        if (!string.IsNullOrEmpty(_email) && !string.IsNullOrEmpty(_password))
        {
            Task.Run(async () => await TryLogin());
        }
        else
        {
            _syncContext.Post(_ => Login(), null);
        }
    }
    
    private void InitializeTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Giriş Yap", null, (s, e) => Login());
        menu.Items.Add("Yazıcı Seç", null, (s, e) => SelectPrinter());
        menu.Items.Add("Çıkış", null, (s, e) => ExitApp());
        
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "MenuBu Yazıcı - Bağlı Değil",
            Visible = true,
            ContextMenuStrip = menu
        };
        
        _pollTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        _pollTimer.Tick += (s, e) => Task.Run(async () => await PollJobs());
    }
    
    private void ExitApp()
    {
        _pollTimer?.Stop();
        _trayIcon?.Dispose();
        Application.Exit();
    }

    private void LoadSettings()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MenuBu", "settings.txt");
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                if (lines.Length >= 3)
                {
                    _email = lines[0];
                    _password = lines[1];
                    _printerName = lines[2];
                }
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MenuBu");
            Directory.CreateDirectory(dir);
            File.WriteAllLines(Path.Combine(dir, "settings.txt"), new[] { _email ?? "", _password ?? "", _printerName ?? "" });
        }
        catch { }
    }

    private void Login()
    {
        var form = new Form 
        { 
            Text = "MenuBu Giriş", 
            Width = 350, 
            Height = 200, 
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        
        var lblEmail = new Label { Text = "Email:", Left = 20, Top = 20, Width = 300 };
        var emailBox = new TextBox { Left = 20, Top = 45, Width = 300, Text = _email };
        var lblPass = new Label { Text = "Şifre:", Left = 20, Top = 75, Width = 300 };
        var passBox = new TextBox { Left = 20, Top = 100, Width = 300, UseSystemPasswordChar = true, Text = _password };
        var btn = new Button { Text = "Giriş Yap", Left = 220, Top = 130, Width = 100 };
        
        btn.Click += (s, e) =>
        {
            _email = emailBox.Text.Trim();
            _password = passBox.Text;
            form.DialogResult = DialogResult.OK;
            form.Close();
        };
        
        form.Controls.AddRange(new Control[] { lblEmail, emailBox, lblPass, passBox, btn });
        
        if (form.ShowDialog() == DialogResult.OK)
        {
            Task.Run(async () => await TryLogin());
        }
    }

    private async Task TryLogin()
    {
        try
        {
            var url = $"https://menubu.com.tr/api/print-jobs.php?email={Uri.EscapeDataString(_email)}&password={Uri.EscapeDataString(_password)}";
            var res = await _http.GetStringAsync(url);
            var doc = JsonDocument.Parse(res);
            
            if (doc.RootElement.TryGetProperty("success", out var s) && s.GetBoolean())
            {
                _businessId = doc.RootElement.GetProperty("business_id").GetInt32();
                _connected = true;
                
                _syncContext.Post(_ =>
                {
                    if (_trayIcon != null)
                    {
                        _trayIcon.Text = "MenuBu Yazıcı - Bağlı";
                        _trayIcon.ShowBalloonTip(2000, "MenuBu", "Bağlantı başarılı", ToolTipIcon.Info);
                    }
                    _pollTimer?.Start();
                }, null);
                
                SaveSettings();
            }
            else
            {
                _syncContext.Post(_ => MessageBox.Show("Giriş başarısız", "Hata"), null);
            }
        }
        catch (Exception ex)
        {
            _syncContext.Post(_ => MessageBox.Show($"Hata: {ex.Message}", "Hata"), null);
        }
    }

    private void SelectPrinter()
    {
        using var dlg = new PrintDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _printerName = dlg.PrinterSettings.PrinterName;
            SaveSettings();
            MessageBox.Show($"Yazıcı seçildi: {_printerName}", "Başarılı");
        }
    }

    private async Task PollJobs()
    {
        if (!_connected) return;
        
        try
        {
            var url = $"https://menubu.com.tr/api/print-jobs.php?email={Uri.EscapeDataString(_email!)}&password={Uri.EscapeDataString(_password!)}";
            var res = await _http.GetStringAsync(url);
            var doc = JsonDocument.Parse(res);
            
            if (doc.RootElement.TryGetProperty("jobs", out var jobs) && jobs.ValueKind == JsonValueKind.Array)
            {
                foreach (var job in jobs.EnumerateArray())
                {
                    var id = job.GetProperty("id").GetInt32();
                    if (_processed.Contains(id)) continue;
                    
                    _processed.Add(id);
                    await PrintJob(id, job.GetProperty("payload").GetString() ?? "{}");
                }
            }
        }
        catch { }
    }

    private async Task PrintJob(int jobId, string payload)
    {
        try
        {
            await UpdateStatus(jobId, "printing");
            
            var doc = JsonDocument.Parse(payload);
            var lines = new List<string>();
            
            if (doc.RootElement.TryGetProperty("order", out var order))
            {
                lines.Add("=== SIPARIŞ ===");
                lines.Add("");
                
                if (order.TryGetProperty("display_id", out var did))
                    lines.Add($"Sipariş No: {did}");
                
                if (order.TryGetProperty("table", out var table))
                    lines.Add($"Masa: {table.GetString()}");
                
                if (order.TryGetProperty("created_at", out var created))
                    lines.Add($"Tarih: {created.GetString()}");
                
                lines.Add("");
                lines.Add("--- ÜRÜNLER ---");
                
                if (order.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var name = item.TryGetProperty("name", out var n) ? n.GetString() : "Ürün";
                        var qty = item.TryGetProperty("quantity", out var q) ? q.GetInt32() : 1;
                        var price = item.TryGetProperty("total", out var p) ? p.GetDecimal() : 0;
                        
                        lines.Add($"{qty}x {name} - {price:F2} TL");
                        
                        if (item.TryGetProperty("note", out var note) && !string.IsNullOrEmpty(note.GetString()))
                            lines.Add($"  Not: {note.GetString()}");
                    }
                }
                
                lines.Add("");
                lines.Add("--- TOPLAM ---");
                
                if (order.TryGetProperty("total", out var total))
                    lines.Add($"Toplam: {total.GetDecimal():F2} TL");
                
                lines.Add("");
                lines.Add("Afiyet Olsun");
            }
            
            Print(lines);
            await UpdateStatus(jobId, "printed");
        }
        catch (Exception ex)
        {
            await UpdateStatus(jobId, "failed", ex.Message);
        }
    }

    private void Print(List<string> lines)
    {
        _syncContext.Post(_ =>
        {
            try
            {
                var pd = new PrintDocument();
                if (!string.IsNullOrEmpty(_printerName))
                    pd.PrinterSettings.PrinterName = _printerName;
                
                pd.PrintPage += (s, e) =>
                {
                    if (e.Graphics == null) return;
                    
                    var font = new Font("Arial", 10);
                    float y = 10;
                    
                    foreach (var line in lines)
                    {
                        e.Graphics.DrawString(line, font, Brushes.Black, 10, y);
                        y += font.GetHeight(e.Graphics);
                    }
                    
                    e.HasMorePages = false;
                };
                
                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yazdırma hatası: {ex.Message}", "Hata");
            }
        }, null);
    }

    private async Task UpdateStatus(int jobId, string status, string? error = null)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { key = _email, status, error_message = error });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            await _http.PostAsync($"https://menubu.com.tr/api/print-jobs.php?id={jobId}", content);
        }
        catch { }
    }
}
