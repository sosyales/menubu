# MenuBu Printer Agent

Windows masaüstü uygulaması - Restoran siparişlerini otomatik olarak termal yazıcılara yazdırır.

## 🚀 Hızlı Başlangıç

### İndirme

[Releases](https://github.com/sosyales/menubu/releases) sayfasından en son sürümü indirin.

### Kurulum

1. `MenuBuPrinterAgent.exe` dosyasını çalıştırın
2. Sistem tepsisinde ikona sağ tıklayın → "Giriş Yap"
3. Email ve şifrenizi girin
4. "Yazıcı Ayarla" ile yazıcınızı seçin

## 📋 Gereksinimler

- Windows 10 veya üzeri
- .NET 6.0 Runtime (uygulama ile birlikte gelir)
- Termal yazıcı (58mm veya 80mm)
- İnternet bağlantısı

## 🔧 Özellikler

### Temel Özellikler
- ✅ **Otomatik Sipariş Yazdırma**: Yeni siparişler anında yazdırılır
- ✅ **Tüm Entegrasyonlar**: Getir, Migros, Trendyol, Yemeksepeti desteği
- ✅ **Self Service & Masa Siparişleri**: Tüm sipariş tipleri destekleniyor
- ✅ **58mm ve 80mm Yazıcılar**: Her iki boyut için optimize edilmiş

### Gelişmiş Özellikler
- ✅ **Çoklu Yazıcı Eşleştirme**: Farklı yazıcılara farklı fişler
- ✅ **Otomatik Yeniden Bağlanma**: Bağlantı kesildiğinde 15 saniye sonra tekrar dener
- ✅ **Otomatik Başlatma**: Windows açılışında otomatik çalışır
- ✅ **Kuyruk Yönetimi**: Bekleyen işleri görüntüleme ve temizleme
- ✅ **Bildirimler**: Her işlem için sistem bildirimleri

## ⚙️ Ayarlar

### Yazıcı Ayarları
- **Yazıcı Seçimi**: Varsayılan veya belirli bir yazıcı seçin
- **Yazıcı Genişliği**: 58mm veya 80mm
- **Font Boyutu**: -3 ile +3 arası ayarlama

### Yazıcı Eşleştirme
- Web panelinden tanımlanan yazıcıları fiziksel yazıcılarla eşleştirin
- Mutfak, adisyon, bar gibi farklı yazıcılar kullanın

## 📖 Detaylı Dokümantasyon

[KURULUM.md](KURULUM.md) dosyasına bakın.

## 🏗️ Geliştirme

```bash
# Projeyi klonla
git clone https://github.com/sosyales/menubu.git
cd menubu/Yazici

# Derle
dotnet build

# Çalıştır
dotnet run
```

## 📦 Build

GitHub Actions otomatik olarak her push'ta derler ve release oluşturur.

## 🔄 GitHub'a Push Etme

```bash
cd /var/www/fastuser/data/www/menubu.com.tr/Yazici

# Değişiklikleri ekle
git add -A

# Commit
git commit -m "Açıklama mesajı"

# Push (SSH key gerekli)
git push origin main
```

**Not:** GitHub'a SSH key eklenmeli:
1. `ssh-keygen -t ed25519 -C "email@example.com"`
2. `cat ~/.ssh/id_ed25519.pub` - Çıktıyı kopyala
3. GitHub → Settings → SSH Keys → New SSH key
4. Yapıştır ve kaydet

## 📝 Lisans

Proprietary - MenuBu © 2025

## 🆘 Destek

Sorun yaşıyorsanız [Issues](https://github.com/sosyales/menubu/issues) açın.
