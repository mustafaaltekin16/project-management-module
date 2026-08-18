# 📱 Mobile

Kurumsal proje ve iş süreci yönetimi için geliştirilmiş, **Flutter** ile yazılmış çoklu platform destekli bir mobil uygulama.
Proje oluşturma, fizibilite/onay süreçleri, Kanban & Gantt görünümleri, görev/doküman/not takibi ve rol bazlı kimlik doğrulama gibi tipik bir kurumsal iş takip uygulamasının uçtan uca akışını sergiler.

<p>
  <img alt="Flutter" src="https://img.shields.io/badge/Flutter-3.x-02569B?logo=flutter&logoColor=white">
  <img alt="Dart" src="https://img.shields.io/badge/Dart-%5E3.12-0175C2?logo=dart&logoColor=white">
  <img alt="Platforms" src="https://img.shields.io/badge/platform-Android%20%7C%20iOS%20%7C%20Windows%20%7C%20macOS%20%7C%20Linux%20%7C%20Web-4c1?">
  <img alt="State Management" src="https://img.shields.io/badge/state-Riverpod-6D4AFF">
  <img alt="License" src="https://img.shields.io/badge/license-Proprietary-lightgrey">
</p>

---

## İçindekiler

- [Öne Çıkan Özellikler](#-öne-çıkan-özellikler)
- [Kullanılan Teknolojiler](#-kullanılan-teknolojiler)
- [Mimari](#-mimari)
- [Proje Yapısı](#-proje-yapısı)
- [Başlarken](#-başlarken)
- [Ortam Değişkenleri](#-ortam-değişkenleri)
- [Kullanışlı Komutlar](#-kullanışlı-komutlar)
- [Test](#-test)
- [Yol Haritası](#-yol-haritası)
- [Lisans](#-lisans)

## ✨ Öne Çıkan Özellikler

- **🔐 Kimlik doğrulama** — JWT tabanlı giriş, `flutter_secure_storage` ile güvenli oturum saklama ve otomatik token yenileme/interceptor akışı.
- **📊 Pano (Dashboard)** — Aktif projeler, özet istatistikler ve kişisel görev/proje kartlarının tek bakışta özeti.
- **📁 Proje Listesi** — Klasik liste görünümünün yanı sıra **Kanban board** ve **Gantt zaman çizelgesi** görünümleriyle projeleri farklı perspektiflerden takip etme.
- **🧾 Proje Oluşturma Sihirbazı** — Basit / çoklu birim / fizibilite tabanlı proje modları, departman & sorumlu ataması, bütçe/para birimi ve ek dosya (attachment) yükleme desteği.
- **📌 Proje Detayı** — Görevler, notlar, dokümanlar, aktivite akışı ve zaman çizelgesi sekmeleriyle tek bir projenin tüm yaşam döngüsü.
- **✅ Fizibilite & Onay Süreci** — Kalem bazlı fizibilite girişleri, onaya gönderme ve çok adımlı onay (approval step) takibi.
- **🌗 Aydınlık / Karanlık Tema** — Material 3 tabanlı, tek bir marka rengi üzerinden türetilmiş tutarlı bir tema sistemi.
- **👤 Profil** — Oturum açan kullanıcının bilgileri ve çıkış işlemleri.

## 🛠 Kullanılan Teknolojiler

| Katman | Paket | Amaç |
| --- | --- | --- |
| State management | [`flutter_riverpod`](https://pub.dev/packages/flutter_riverpod) | Reaktif state yönetimi ve bağımlılık enjeksiyonu |
| Routing | [`go_router`](https://pub.dev/packages/go_router) | Declarative, branch tabanlı (bottom navigation ile uyumlu) yönlendirme |
| Ağ katmanı | [`dio`](https://pub.dev/packages/dio) | HTTP istemcisi, interceptor tabanlı auth header yönetimi |
| Kimlik doğrulama | [`jwt_decoder`](https://pub.dev/packages/jwt_decoder), [`flutter_secure_storage`](https://pub.dev/packages/flutter_secure_storage) | Token çözümleme ve güvenli, şifreli yerel depolama |
| Yapılandırma | [`flutter_dotenv`](https://pub.dev/packages/flutter_dotenv) | Ortama göre değişen API adresi gibi ayarların `.env` üzerinden yönetimi |
| Dosya işlemleri | [`file_selector`](https://pub.dev/packages/file_selector) | Proje eklerinin seçilmesi (çoklu platform dosya seçici) |
| Yerelleştirme | [`intl`](https://pub.dev/packages/intl) | Türkçe tarih/sayı biçimlendirme |

## 🏗 Mimari

Uygulama, her özelliğin kendi içinde **data / domain / presentation** katmanlarına ayrıldığı **özellik odaklı (feature-first)** bir mimariyle organize edilmiştir:

```
UI (screens/widgets)
   ↓ watch/read
Controller (Riverpod Notifier)
   ↓
Repository (domain arayüzü)
   ↓
API Service (dio) → Backend REST API
```

- **`core/`** — Uygulama genelinde paylaşılan alt yapı: ağ katmanı (Dio client + interceptor), router, güvenli depolama, tema ve ortak widget'lar.
- **`features/`** — Her biri kendi `data/`, `domain/` ve `presentation/` klasörlerine sahip bağımsız modüller (`login`, `dashboard`, `projects`, `project_create`, `project_detail`, `feasibility`, `profile`).
- **`shared/`** — Birden fazla özelliğin ortak kullandığı gezinme (navigation) bileşenleri.

Bu ayrım sayesinde her özellik bağımsız test edilebilir ve backend/API sözleşmesi değiştiğinde sadece ilgili `data` katmanı güncellenir.

## 📂 Proje Yapısı

```
lib/
├── app.dart                     # MaterialApp.router kurulumu, tema bağlama
├── main.dart                    # Uygulama giriş noktası, .env ve locale init
├── core/
│   ├── api/                      # Ortak API response modelleri
│   ├── config/                   # AppConfig (API_BASE_URL vb.)
│   ├── network/                  # Dio client + auth interceptor
│   ├── router/                   # go_router tanımları ve route sabitleri
│   ├── storage/                  # flutter_secure_storage sarmalayıcısı
│   ├── theme/                    # AppTheme, renkler, spacing/radius token'ları
│   └── widgets/                  # Paylaşılan UI bileşenleri (empty/error/loading state)
├── features/
│   ├── login/                    # Giriş ekranı, auth repository & controller
│   ├── dashboard/                 # Pano ekranı ve istatistik kartları
│   ├── projects/                  # Proje listesi, Kanban, Gantt, repository
│   ├── project_create/            # Proje oluşturma sihirbazı
│   ├── project_detail/            # Görev/not/doküman/aktivite sekmeleri
│   ├── feasibility/                # Fizibilite kalemleri ve onay akışı
│   └── profile/                   # Kullanıcı profili
└── shared/
    └── navigation/                # Alt gezinme çubuğu (MainShell)
```

## 🚀 Başlarken

### Gereksinimler

- [Flutter SDK](https://docs.flutter.dev/get-started/install) (stable kanal, Dart `^3.12.2`)
- Bağlanılacak bir backend REST API adresi (kendi API'niz veya geliştirme ortamı)

### Kurulum

```bash
# Depoyu klonlayın
git clone https://github.com/<kullanici-adi>/ozveri-mobile.git
cd ozveri-mobile

# Bağımlılıkları yükleyin
flutter pub get

# Ortam dosyasını oluşturun ve API adresini kendinize göre düzenleyin
cp .env.example .env

# Uygulamayı çalıştırın (bağlı cihaz/emülatör/tarayıcı otomatik seçilir)
flutter run
```

> 💡 Android emülatöründen host makinedeki bir API'ye erişmek için `10.0.2.2` adresini, gerçek cihazdan test ederken ise makinenizin yerel ağ IP adresini kullanabilirsiniz.

## 🔧 Ortam Değişkenleri

Uygulama, yapılandırmasını proje kökündeki `.env` dosyasından okur (`.env.example` şablon olarak eklenmiştir ve `.env` `.gitignore` ile sürüm kontrolü dışında tutulur):

| Değişken | Açıklama | Örnek |
| --- | --- | --- |
| `API_BASE_URL` | Backend REST API'nin kök adresi | `http://10.0.2.2:7000` |

## 📜 Kullanışlı Komutlar

```bash
flutter analyze     # Statik analiz / lint kontrolü
flutter test         # Widget ve unit testlerini çalıştırır
flutter build apk    # Android için release paketi
flutter build ios    # iOS için release paketi
```

## ✅ Test

```bash
flutter test
```

Mevcut test paketi, oturum kapalıyken uygulamanın giriş ekranını doğru şekilde gösterdiğini doğrulayan bir widget testi içerir (`test/widget_test.dart`).

## 🗺 Yol Haritası

- [ ] Özellik modülleri için birim/entegrasyon test kapsamının genişletilmesi
- [ ] Push bildirimleri
- [ ] Offline destek / yerel önbellekleme
- [ ] CI (GitHub Actions) ile otomatik `analyze` + `test` kontrolü

## 📄 Lisans

Bu proje kurumsal/özel kullanım için geliştirilmiştir; açık kaynak lisansı ile dağıtılmamaktadır.
Tüm hakları saklıdır (bkz. [kök README](../README.md#-lisans)).
