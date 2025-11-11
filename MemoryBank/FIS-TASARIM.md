# FİŞ TASARIMI - MEMORY BANK

## 📍 Fiş Tasarımı Nerede?

**Dosya:** `/api/orders/queue-print.php`
**Satırlar:** 47-189

## 🎨 Mevcut Fiş Yapısı

### 1. HEADER (Üst Bilgiler)
```
Satır 47-50: Tarih/Saat ve Adisyon No
Satır 52: İşletme Adı (BÜYÜK HARF)
Satır 54-59: "MASA SİPARİŞİ" veya "TESLİMAT BİLGİLERİ"
```

### 2. MÜŞTERİ BİLGİLERİ
```
Satır 61-63: Ödeme Yöntemi
Satır 64-66: Müşteri Adı
Satır 67-69: Telefon
Satır 71-74: Adres
Satır 76-79: Teslimat Zamanı
Satır 81-85: Onay Kodu
```

### 3. PLATFORM BİLGİLERİ (Entegrasyon)
```
Satır 88-90: Kanal (Getir, Yemeksepeti, vb.)
Satır 91-93: Restoran/Şube Adı
Satır 94-96: Platform Sipariş ID
```

### 4. MASA BİLGİSİ
```
Satır 98-102: Masa ve Alan Bilgisi
```

### 5. MÜŞTERİ NOTU
```
Satır 104-107: Sipariş Notu
```

### 6. ÜRÜNLER TABLOSU
```
Satır 109-111: Tablo Başlığı ("Ad Ürün" - "Fiyat")
Satır 112: Ayırıcı çizgi

Satır 114-157: Her ürün için:
  - Adet + Ürün Adı + Fiyat
  - Alt çizgi
  - Opsiyonlar (→ işaretiyle)
  - Ürün notu
```

### 7. TOPLAM VE ÖDEME
```
Satır 159-161: TOPLAM
Satır 163-172: Ödeme Detayı
  - Ödeme yöntemi
  - Tutar
  - Tahsil
  - Kalan
```

### 8. FOOTER
```
Satır 174: Sipariş No
Satır 176: Ayırıcı çizgi
Satır 177: "Afiyet Olsun."
Satır 178: "MenuBu ♥"
```

## 📐 Teknik Detaylar

**Genişlik:** `$maxWidth = 28` karakter (58mm yazıcı için)
**Format:** `lines` array (string array)
**Encoding:** UTF-8

## 🔧 Değişiklik Yapma

### Örnek 1: Başlık Değiştirme
```php
// Satır 52
$lines[] = strtoupper($business['name']);

// Değiştir:
$lines[] = '*** ' . strtoupper($business['name']) . ' ***';
```

### Örnek 2: Footer Değiştirme
```php
// Satır 177-178
$lines[] = 'Afiyet Olsun.';
$lines[] = 'MenuBu ♥';

// Değiştir:
$lines[] = 'Teşekkür Ederiz!';
$lines[] = 'www.menubu.com.tr';
```

### Örnek 3: Yeni Alan Ekleme
```php
// Satır 107'den sonra ekle:
if (!empty($order['special_field'])) {
    $lines[] = '';
    $lines[] = 'Özel Alan: ' . $order['special_field'];
}
```

## 🎯 Önemli Notlar

1. **Genişlik Kontrolü:** Her satır max 28 karakter olmalı
2. **Boş Satır:** `$lines[] = '';` ile boş satır ekle
3. **Hizalama:** `str_repeat(' ', $count)` ile boşluk ekle
4. **Kalın Yazı:** C# kodu büyük harfleri otomatik kalın yapar
5. **Ayırıcı:** `str_repeat('-', $maxWidth)` veya `str_repeat('_', $maxWidth)`

## 📊 Veri Kaynakları

### Orders Tablosu
- `id` - Sipariş ID
- `created_at` - Tarih/Saat
- `customer_name` - Müşteri Adı
- `customer_phone` - Telefon
- `customer_address` - Adres
- `customer_note` - Müşteri Notu
- `payment_method` - Ödeme Yöntemi
- `total_amount` - Toplam Tutar
- `table_number` - Masa No
- `platform` - Platform (Getir, Yemeksepeti)
- `platform_order_id` - Platform Sipariş ID
- `branch_name` - Şube Adı
- `confirmation_code` - Onay Kodu
- `delivery_time` - Teslimat Zamanı

### Order Items Tablosu
- `product_name` - Ürün Adı
- `quantity` - Adet
- `subtotal` - Ara Toplam
- `options` - Opsiyonlar (JSON)
- `notes` - Ürün Notu

## 🔄 İş Akışı

1. Sipariş oluşturulur
2. `queue-print.php` çağrılır
3. Sipariş bilgileri çekilir
4. `$lines` array'i oluşturulur
5. `print_jobs` tablosuna eklenir
6. Yazıcı ajanı job'ı çeker
7. C# kodu yazdırır

## 📝 Test Etme

**Test Yazdırma:**
```
POST /panel/printer_settings.php
name="test_print"
```

**Sipariş Yazdırma:**
```
GET /api/orders/queue-print.php?id=123&type=receipt
```

## 🚀 Gelecek İyileştirmeler

- [ ] Veritabanından template çekme
- [ ] Kullanıcı özelleştirilebilir alanlar
- [ ] Çoklu dil desteği
- [ ] Logo ekleme
- [ ] QR kod ekleme
- [ ] Barkod ekleme

---

**Son Güncelleme:** 12 Kasım 2025
**Dosya Versiyonu:** 1.0
