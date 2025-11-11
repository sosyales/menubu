# MenuBu Yazıcı Ajanı

Basit ve temiz Windows yazıcı ajanı.

## Özellikler
- Email/şifre ile giriş
- Otomatik print job polling
- Basit fiş yazdırma
- Yazıcı seçimi

## Kullanım
1. Uygulamayı çalıştırın
2. System tray'den "Giriş Yap" seçin
3. Email ve şifrenizi girin
4. "Yazıcı Seç" ile yazıcınızı seçin
5. Otomatik yazdırma başlar

## Build
```bash
dotnet publish -c Release -r win-x64 --self-contained
```
