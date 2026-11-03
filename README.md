# Dispatch

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-API%201.0.323-orange)

**Dispatch**, Counter-Strike 2 sunucularında çalışan bir CounterStrikeSharp eklentisidir. Config'de belirttiğiniz eklenti klasörünü ZIP arşivine dönüştürür, Filebin'e yükler ve indirme bağlantısını sunucu konsoluna yazar.

Özellikle dosya yöneticisine erişmeden, web panelinin sunucu konsolundan bir eklenti klasörünü indirmek için kullanılabilir. Daha önce **PluginGonder** adıyla kullanılan eklentinin Dispatch sürümüdür.

## Nasıl çalışır?

```text
Dispatch.json → PluginAdi → css_dispatch → ZIP → Filebin → konsolda indirme bağlantısı
```

- Hedef klasördeki dosyaları ve alt klasörlerdeki dosyaları arşivler.
- ZIP içinde hedef klasörün adını korur: `AFKManager/AFKManager.dll` gibi.
- Arşivi bellekte oluşturur; sunucu diskine geçici ZIP yazmaz.
- ZIP oluşturma ve HTTP yükleme işlemlerini arka planda çalıştırır.
- Sonucu sunucu konsoluna, komutu bir oyuncu çalıştırdıysa ayrıca oyuncunun konsoluna yazar.
- Oyuncu tarafından kullanıldığında `@css/root` yetkisini kontrol eder.

## Gereksinimler

- Metamod:Source ve [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) kurulu bir CS2 sunucusu.
- Eklentinin hedefi: **.NET 8**. Sunucunun CounterStrikeSharp çalışma ortamı bu hedefi desteklemelidir.
- Projede kullanılan NuGet paketi: **CounterStrikeSharp.API 1.0.323**. Tüm sunucu/API sürümleriyle uyumluluk ayrıca test edilmemiştir.
- Sunucudan `https://filebin.net` adresine HTTPS erişimi.
- Kaynaktan derlemek için .NET 8 hedefini derleyebilen bir .NET SDK ve NuGet erişimi.

## Derleme

Depoyu klonladıktan sonra proje klasöründe çalıştırın:

```sh
git clone https://github.com/TheDarkGuardian/Dispatch.git
cd Dispatch
dotnet restore Dispatch.csproj
dotnet build Dispatch.csproj -c Release --no-restore
```

Derlenen dosyalar `bin/Release/net8.0/` altında oluşur. Sunucuya kurulacak temel dosyalar `Dispatch.dll` ve `Dispatch.deps.json` dosyalarıdır. `Dispatch.pdb` hata ayıklama için isteğe bağlıdır.

## Kurulum

1. Sunucuda aşağıdaki klasörü oluşturun:

   ```text
   game/csgo/addons/counterstrikesharp/plugins/Dispatch/
   ```

2. Derlenen `Dispatch.dll` ve `Dispatch.deps.json` dosyalarını bu klasöre kopyalayın. Sunucudaki CounterStrikeSharp kurulumunun sağladığı `CounterStrikeSharp.API.dll` dosyasını ayrıca eklenti klasörüne kopyalamayın.
3. Sunucuyu yeniden başlatın veya sunucu konsolunda çalıştırın:

   ```text
   css_plugins load Dispatch
   ```

4. İlk yüklemede oluşturulan config dosyasını düzenleyin:

   ```text
   game/csgo/addons/counterstrikesharp/configs/plugins/Dispatch/Dispatch.json
   ```

Örnek klasör yapısı:

```text
game/csgo/addons/counterstrikesharp/
├── plugins/
│   ├── Dispatch/
│   │   ├── Dispatch.dll
│   │   └── Dispatch.deps.json
│   └── AFKManager/
│       └── AFKManager.dll
└── configs/plugins/Dispatch/
    └── Dispatch.json
```

## Yapılandırma

[`config/Dispatch.example.json`](config/Dispatch.example.json) dosyasında örnek yapılandırma bulunur:

```json
{
  "PluginAdi": "AFKManager",
  "ConfigVersion": 1
}
```

| Alan | Açıklama | Varsayılan |
| --- | --- | --- |
| `PluginAdi` | `plugins/` altındaki, ZIP'lenip yüklenecek klasörün adı. | `AFKManager` |
| `ConfigVersion` | CounterStrikeSharp tarafından kullanılan config sürümü. | `1` |

`PluginAdi` alanına yalnızca klasör adını yazın; `.dll` uzantısı veya tam dosya yolu yazmayın. Linux sunucularda büyük/küçük harf eşleşmesine dikkat edin.

Config değişikliğinden sonra sunucu konsolundan eklentiyi yeniden yükleyin:

```text
css_plugins reload Dispatch
```

## Kullanım

Web panelinizin **sunucu konsolunda** veya RCON üzerinden:

```text
css_dispatch
```

Oyuncu sohbetinden:

```text
!dispatch
```

Oyuncunun `@css/root` yetkisi olmalıdır. Sunucu konsolundan çalıştırıldığında oyuncu yetkisi aranmaz. RCON kullanıyorsanız erişimi sunucunun RCON kimlik doğrulaması belirler.

Başarılı yükleme sonunda örnek çıktı:

```text
[Dispatch] BASARILI! 'AFKManager' yuklendi.
[Dispatch] Indirme linki:
[Dispatch] https://filebin.net/<bin-id>/AFKManager.zip
```

Her komut çalıştırıldığında yeni bir Filebin kimliği oluşturulur. Hedef klasördeki dosyalar yerinde kalır.

## Kapsam ve sınırlar

- Yalnızca seçilen `plugins/<PluginAdi>/` klasörü arşivlenir. `configs/plugins/` gibi başka konumlardaki ayarlar otomatik olarak eklenmez.
- Hedef klasörün içindeki tüm dosyalar yüklenir; filtreleme veya şifreleme uygulanmaz. Paylaşılmaması gereken anahtar, parola ve özel dosyaları hedef klasörden çıkarın. İndirme bağlantısını bilen kişiler arşive erişebilir.
- ZIP bellekte tutulur ve yükleme için bir kopyası daha oluşturulur. Büyük klasörler sunucunun bellek kullanımını artırır.
- HTTP isteğinin zaman aşımı 60 saniyedir. Otomatik yeniden deneme, yükleme kuyruğu ve eşzamanlı komut sınırı bulunmaz.
- Filebin geçici bir dosya paylaşım hizmetidir. Linkin ömrü ve hizmet limitleri Filebin tarafından belirlenir; konsoldaki yaklaşık bir günlük süre garanti değildir.
- Kaynak kod Release modunda derlenerek kontrol edilmiştir. Canlı CS2 komutu ve gerçek Filebin yüklemesi ayrıca sunucuda doğrulanmalıdır.

## Sorun giderme

| Durum | Kontrol |
| --- | --- |
| `Unknown command: css_dispatch` | `css_plugins list` ile Dispatch'in yüklü olduğunu ve sunucu loglarını kontrol edin. |
| `Klasor bulunamadi` | `PluginAdi` değerinin `plugins/` altındaki gerçek klasör adıyla eşleştiğini kontrol edin. |
| Yetki hatası | Oyuncunun `@css/root` yetkisini kontrol edin veya sunucu konsolunu kullanın. |
| HTTP hatası / zaman aşımı | Sunucunun Filebin'e HTTPS erişimini ve Filebin'in hizmet durumunu kontrol edin. |
| Config değişikliği uygulanmadı | JSON biçimini doğrulayın, ardından `css_plugins reload Dispatch` çalıştırın. |

## Lisans

[MIT](LICENSE) — Copyright (c) 2026 Arda Baran Akdemir.
