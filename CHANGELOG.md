# MenuBu Printer Agent - Değişiklik Geçmişi

## Son Yapılan İyileştirmeler (11 Ocak 2025)

### Fiş Tasarımı Tamamen Yenilendi
- ✅ HTML tabanlı modern tasarım düz metne çevrildi
- ✅ 58mm termal yazıcı için optimize edildi (32 karakter genişlik)
- ✅ Tarih ve Adisyon No üstte yan yana
- ✅ İşletme adı ortalı ve kalın
- ✅ "TESLİMAT BİLGİLERİ" / "MASA SİPARİŞİ" başlıkları
- ✅ Müşteri bilgileri düzenli formatta
- ✅ Ürünler tablo formatında (Adet x Ürün Adı - Fiyat)
- ✅ Opsiyonlar → işaretiyle gösteriliyor
- ✅ Opsiyon fiyatları ürün fiyatına dahil
- ✅ TOPLAM kalın ve belirgin
- ✅ Ödeme Detayı bölümü eklendi
- ✅ Alt kısımda Sipariş No
- ✅ "Afiyet Olsun." ve "MenuBu ♥" footer

### Entegrasyon Özellikleri
- ✅ Platform bilgisi (Getir, Yemeksepeti, vb.)
- ✅ Platform Sipariş ID
- ✅ Şube bilgisi
- ✅ Onay kodu (varsa)
- ✅ İndirim miktarı
- ✅ Teslimat ücreti

### Otomatik Kalın Yazı Algılama (C#)
- ✅ Büyük harfle yazılan metinler otomatik kalın
- ✅ Başlıklar (Sipariş No:, Tarih:, Müşteri:, vb.) otomatik kalın
- ✅ Ürün satırları otomatik kalın
- ✅ TOPLAM otomatik kalın
- ✅ `**` işaretleri kaldırıldı (artık gerek yok)

### Teknik İyileştirmeler
- ✅ InvalidOperationException hatası çözüldü
- ✅ "lines" formatı kullanılıyor (kararlı ve hızlı)
- ✅ Uzun metinler otomatik kısaltılıyor (..)
- ✅ Genişlik optimizasyonu (32 karakter)
- ✅ Test fişi yeni tasarımda

## Dosya Yapısı

### API
- `/api/orders/queue-print.php` - Sipariş yazdırma (yeni tasarım)
- `/api/print-jobs.php` - Print job yönetimi
- `/api/print-auth.php` - Yazıcı kimlik doğrulama

### Panel
- `/panel/printer_settings.php` - Yazıcı ayarları ve test fişi

### C# Yazıcı Ajanı
- `Printing/PrinterManager.cs` - Otomatik kalın algılama
- `TrayApplicationContext.cs` - Ana uygulama
- `Services/MenuBuApiClient.cs` - API iletişimi

## GitHub
- Repository: https://github.com/sosyales/menubu
- Actions: Otomatik build her push'ta
- Artifacts: ZIP dosyası Actions sekmesinden indirilebilir

## Test Bilgileri
- Test kullanıcı: anamuralem@gmail.com / Nazmi33!
- Business ID: 1
- Test fişi: Panel → Yazıcı Ayarları → Test Siparişi Yazdır

## Önemli Notlar
- Yazıcı ajanı Windows'ta sürekli çalışmalı
- 6 saniyede bir API'yi kontrol eder
- Kalın yazı için yeni build gerekli (GitHub Actions)
- 58mm yazıcı için 32 karakter genişlik optimal
