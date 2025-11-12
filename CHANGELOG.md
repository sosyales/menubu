# Değişiklik Günlüğü

## v2.1.0 - HTML Yazdırma ve İyileştirmeler

### ✨ Yeni Özellikler
- **WebView2 ile HTML Yazdırma**: Artık print.php'deki HTML tasarımı direkt yazdırılıyor
- **Otomatik Yeniden Bağlanma**: Bağlantı kesildiğinde 30 saniye sonra otomatik tekrar deneme
- **Bağlantı Bildirimleri**: Bağlantı kesildiğinde ve geri geldiğinde bildirim
- **Balloon Tip Tıklama**: Bağlantı kesildi bildirimine tıklayarak yeniden bağlanma

### 🔧 İyileştirmeler
- HTML tasarımı 58mm ve 80mm için otomatik optimize ediliyor
- Uygulama her zaman sistem tray'de açık kalıyor
- Daha iyi hata mesajları ve kullanıcı bildirimleri

### 🐛 Düzeltmeler
- Metin kesme sorunu çözüldü
- Sağa yaslama sorunu düzeltildi
- Ürün opsiyonları ve fiyatları tam gösteriliyor

### 📦 Teknik Değişiklikler
- Microsoft.Web.WebView2 paketi eklendi
- HtmlPrinter sınıfı oluşturuldu
- PrinterManager IDisposable implement edildi
- Otomatik yeniden bağlanma mekanizması eklendi

### 🔄 API Değişiklikleri
- queue-print.php artık HTML payload gönderiyor
- Yazıcı ajanı hem `lines` hem `html` formatını destekliyor
