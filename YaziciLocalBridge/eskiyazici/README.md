# MenuBu Yazıcı Ajanı (Windows)

Windows tepsi ajansı; MenuBu web servisine (email/şifre) bağlanır, yeni siparişleri otomatik yazdırır ve bekleyen işleri açılışta onaya sunar.

## Özellikler

- Tray ikonuna sahip Windows Forms uygulaması (saat yanında çalışır)
- Giriş, çıkış, yeniden bağlan ve yazıcı ayarlama menüleri
- İlk açılışta bekleyen işleri sorar; yeni işler otomatik yazdırılır
- Varsayılan yazıcıya veya seçili yazıcıya gönderim
- Bağlantı koptuğunda uyarı verir, yeniden bağlandığında bilgilendirir
- Otomatik başlangıç (giriş başarılı olduğunda kullanıcı oturumuna eklenir)
- Adrese teslim siparişler için platform/ müşteri detaylarını içeren geliştirilmiş fiş şablonu
- Aynı işin iki kez yazdırılmasını engelleyen geliştirilmiş kuyruk yönetimi ve manuel kuyruk temizleme
- Adrese teslim siparişler için platform/ müşteri detaylarını içeren modern fiş şablonu
- Aynı siparişin çift yazdırılmasını engelleyen gelişmiş kuyruk kontrolü

## Geliştirme Gereksinimleri

- Windows 10 20H1 (19041) veya üzeri
- .NET 8.0 SDK

## Yerel Derleme

```bash
dotnet restore MenuBuPrinterAgent.csproj
dotnet publish MenuBuPrinterAgent.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

`publish` klasöründe tek dosya `MenuBuPrinterAgent.exe` oluşur; Windows'ta ekstra kurulum gerektirmeden çalışır.

> Not: Windows uygulama simgeleri `.ico` formatı gerektirir. `icon.png` tepsi ikonu olarak gömülmüştür; exe ikonu için PNG'den `.ico`ya dönüştürülmüş bir dosyayı `ApplicationIcon` özelliğine bağlayabilirsiniz.

## Örnek Veri

- `samples/delivery_receipt.json` dosyası, adrese teslim/platform siparişleri için yeni fiş şablonunun beklediği JSON formatını gösterir. Paneldeki test yazdırma aracına bu içeriği göndererek tasarımı doğrulayabilirsiniz.

## Örnek Veri

- `samples/delivery_receipt.json` dosyası, adrese teslim/platform siparişleri için yeni fiş şablonunun beklediği JSON formatını gösterir. Paneldeki test yazdırma aracına bu içeriği göndererek tasarımı doğrulayabilirsiniz.

## GitHub Actions

`.github/workflows/build.yml` dosyası depoya push veya pull request açıldığında:

1. Kaynak kodu indirir
2. .NET 8 kurar
3. Projeyi self-contained tek dosya olarak yayınlar
4. Çıktıyı `MenuBuPrinterAgent-win-x64` adlı artefakt olarak yükler

## Dosya Yapısı

- `TrayApplicationContext.cs` — Tepsi uygulamasının ana mantığı
- `Services/MenuBuApiClient.cs` — MenuBu API istemcisi
- `Printing/PrinterManager.cs` — Yazdırma katmanı
- `UI/` — Giriş ve yazıcı ayar formları

Sorularınız için issue açabilirsiniz.
