# ⚙️ Proje Yönetimi Modülü — Backend

<p>
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white">
  <img alt="YARP" src="https://img.shields.io/badge/Gateway-YARP-673AB7">
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white">
  <img alt="RabbitMQ" src="https://img.shields.io/badge/RabbitMQ-MassTransit-FF6600?logo=rabbitmq&logoColor=white">
  <img alt="OpenSearch" src="https://img.shields.io/badge/OpenSearch-Serilog-005EB8?logo=opensearch&logoColor=white">
</p>

Proje Yönetimi modülünün backend'i. Proje kartındaki mikroservis mimarisini birebir uygular: her iş alanı (proje, görev, fizibilite, yapay zekâ) kendi
veritabanına sahip, bağımsız olarak derlenip çalıştırılabilen ayrı bir .NET servisidir.

Bu backend, [`web/`](../web/) altındaki Angular modülünden ve [`mobile/`](../mobile/) altındaki
Flutter modülünden **bağımsız ve ayrı** olarak, sıfırdan geliştirilmiştir; istemcilerle iletişim
sadece HTTP üzerinden (API Gateway) kurulur.

## Mimari

```
İstemci (Angular / mobil / vb.)
        │
        ▼
  Ozdilek.PM.ApiGateway (YARP, tek giriş noktası, port 7500 — bkz. GATEWAY_PORT)
        │
   ┌────┼────────────┬────────────────┐
   ▼    ▼             ▼                ▼
Project Task      Feasibility      AIGateway         RabbitMQ
Service Service    Service          Service          (MassTransit)
(6001)  (6002)     (6003)           (6004)           (6672/mgmt 16672)
        │                             │                  ▲
        ▼                             └──────────────────┘
   Postgres (her servis kendi veritabanı:      (AIGatewayService → RabbitMQ →
   cwa_pm_project, cwa_pm_task,                 TaskService — onaylanan AI
   cwa_pm_feasibility, cwa_pm_ai)                önerisi asenkron olarak göreve dönüşür)
        │
        ▼
   Elasticsearch + Kibana (merkezi log toplama/görselleştirme)
```

Her servis Clean Architecture katmanlarına sahiptir: `Domain` → `Application` → `Infrastructure` → `Api`.
Ortak altyapı (`src/BuildingBlocks/Ozdilek.PM.BuildingBlocks`): JWT doğrulama, Serilog/Elasticsearch
kurulumu, MassTransit/RabbitMQ kurulumu, merkezi hata/response middleware'i, servisler-arası bearer
token forwarding. Bağımlılıksız domain temel sınıfları (`BaseEntity`, `DomainException`,
`IRepository<T>`, KVKK regex filtresi) ise `src/BuildingBlocks/Ozdilek.PM.SharedKernel` içinde —
Domain/Application katmanları framework'e (ASP.NET Core, EF Core, MassTransit) hiç bağımlı değildir.

- **Gateway portu 7500 (6000 değil, varsayılan 7000'den de taşındı).** Chrome/Chromium 6000 numaralı
  portu (X11) tarayıcı güvenliği gereği `ERR_UNSAFE_PORT` ile bloke ediyor — bu, gerçek bir tarayıcıda
  test edilmeden fark edilmez. 7000, sunucuda başka projelerin kullandığı 4200/5010 ile çakışmayı
  önlemek için `GATEWAY_PORT` ortam değişkeniyle (varsayılan 7500) değiştirilebilir hale getirildi;
  frontend'in Angular dev sunucu portu da aynı sebeple 4200'den 4300'e taşındı (`angular.json`'daki
  `serve.options.port`).

## Önemli tasarım kararları ve gerekçeleri

- **Kimlik doğrulama sadece token doğrulama.** Bu modül kendi login/kullanıcı veritabanını kurmaz;
  kurumun mevcut (veya ileride kurulacak) merkezi OIDC sağlayıcısından (Azure AD/Entra, Keycloak,
  IdentityServer vb.) gelen JWT'leri doğrular (`Auth:Mode=ExternalOidc`, `Auth:Authority=...`).
  Bu ortamda gerçek bir authority olmadığından, varsayılan `Auth:Mode=Dev` ile yerel bir simetrik
  anahtarla imzalanmış test token'ları kullanılır — `POST /dev/token` (sadece Gateway'de, sadece
  `Mode=Dev` iken aktif) `{ "userId", "displayName", "roles": [...] }` alır, imzalı bir JWT döner.
  Üretimde tek değişiklik: `Auth:Mode=ExternalOidc` + gerçek `Authority` URL'i.
- **Gerçek mikroservisler**, tek bir "modüler monolit" değil: proje kartındaki "her iş alanı bağımsız
  servisler halinde geliştirilmiştir" ifadesi birebir uygulanmıştır — 4 iş servisi + gateway, her biri
  kendi Dockerfile'ı, kendi veritabanı, kendi `dotnet run`'ı ile bağımsız çalışabilir.
  Servisler arası senkron ihtiyaç (AIGatewayService'in proje bilgisini ProjectService'ten okuması) HTTP
  ile, kullanıcının kendi bearer token'ı forward edilerek yapılır (ayrı bir client-credentials akışına
  gerek yok — bkz. `BearerTokenForwardingHandler`).
- **Mesajlaşma: RabbitMQ + MassTransit.** AI önerisi onaylandığında görev oluşturma, senkron HTTP değil,
  gerçek bir asenkron/event-driven akış: `AIGatewayService` bir `WorkPackageApprovedEvent` yayınlar,
  `TaskService`'teki `WorkPackageApprovedConsumer` bunu dinleyip görevleri oluşturur (AI rozeti ile).
- **AI: LLM Soyutlama Katmanı.** `ILlmProvider` arayüzü; `RagLlmProvider` (varsayılan) iş paketi JSON'unu
  paid bir LLM API'sinden değil, doküman Q&A için zaten kullanılan aynı self-hosted RAG servisinden
  (Weaviate + Haystack + vLLM/Qwen3-VL) üretir — prompt ephemeral bir RAG oturumuna doküman olarak
  yüklenip tek soruluk bir `/qa/ask` ile yanıtlanır. `Mock` (key/ağ gerektirmez, çevrimdışı dev/test için)
  tek alternatif (`Ai:Provider` config anahtarıyla seçilir).
- **KVKK/PII filtreleme, iki katmanlı:** (1) `PromptSanitizationMiddleware`, `/api/ai-suggestions`
  isteklerinin ham gövdesini denetler; (2) `AiSuggestionAppService`, LLM'e gönderilecek **tam
  birleştirilmiş prompt'u** (sunucu tarafından getirilen proje açıklamasını da içerir) ayrıca denetler —
  çünkü middleware sadece bu isteğin gövdesini görebilir, ProjectService'ten gelen veriyi göremez.
  Tespit edilen kategoriler (TCKN, e-posta, telefon, IBAN, kredi kartı) `prompt_audit_log` tablosuna
  **redaksiyon sonrası** haliyle yazılır — ham veri hiçbir zaman loglanmaz.
- **Elasticsearch/Kibana yerine OpenSearch/OpenSearch Dashboards.** Proje kartı Elasticsearch + Kibana
  istiyor; bu ortamda `docker.elastic.co` kayıt sunucusuna (API) ulaşılabiliyor ama gerçek bir
  `docker compose up` denemesinde imaj indirme sırasında CDN katmanı bağlantıyı EOF ile kesti (test
  edilip doğrulandı, varsayım değil). Bu yüzden — işlevsel olarak eşdeğer, aynı REST/bulk protokolünü
  konuşan, Docker Hub'da barınan açık kaynak **OpenSearch + OpenSearch Dashboards** kullanılıyor
  (`Serilog.Sinks.OpenSearch`). Config anahtarı yine de `Serilog:ElasticsearchUrl` — gerçek Elastic'e
  dönmek istenirse `CwaLoggingExtensions.cs`'teki sink çağrısı ve `docker-compose.yml`'deki imaj adları
  tek değişiklik noktasıdır. `Serilog:ElasticsearchUrl` boşsa (yerel `dotnet run` sırasında olduğu gibi)
  sadece console/dosyaya loglanır.

## Yerel Çalıştırma

### Docker Compose (önerilen — tüm yığın)

```bash
cp .env.example .env   # RAG_BASE_URL / RAG_API_KEY / GATEWAY_PORT değerlerini doldurun
docker compose up --build
```

`.env` docker compose tarafından bu klasörden otomatik yüklenir ve **repoya commit edilmez**
(bkz. `.gitignore`).

- Gateway: http://localhost:7500/health
- Project Service: http://localhost:6001/health
- Task Service: http://localhost:6002/health
- Feasibility Service: http://localhost:6003/health
- AI Gateway Service: http://localhost:6004/health
- RabbitMQ Management: http://localhost:16672 (`cwa` / `cwa-dev-password`)
- Kibana: http://localhost:6601

Test token almak için:

```bash
curl -X POST http://localhost:7500/dev/token \
  -H "Content-Type: application/json" \
  -d '{"userId":"u1","displayName":"Ahmet Görür","roles":["Admin","ProjectManager"]}'
```

Dönen `accessToken`'ı `Authorization: Bearer <token>` başlığıyla gateway üzerinden gönderin.

### SDK ile (Docker olmadan)

```bash
dotnet build Ozdilek.PM.slnx
dotnet run --project src/Services/ProjectService/Ozdilek.PM.ProjectService.Api
# diğer servisler ayrı terminallerde benzer şekilde; Postgres/RabbitMQ için Docker gerekir:
docker compose up postgres rabbitmq -d
```

### Testler

```bash
dotnet test Ozdilek.PM.slnx
```

43 birim testi: onay state machine'i (Feasibility), görev bağımlılık döngü kontrolü (Task), proje/şablon
doğrulama kuralları (Project), KVKK regex filtresi + prompt şablonu + AI onay guard'ı (AIGateway).

### RAG servisine bağlama

`Rag:BaseUrl`'i gerçek RAG dağıtımının adresiyle doldurun (appsettings veya `Rag__BaseUrl` ortam
değişkeniyle — RunPod pod'ları yeniden başladığında proxy URL'i değişebilir). `Ai:Provider` zaten
varsayılan olarak `RAG`'dır, ek bir ayar gerekmez.

## Bilinen Sınırlamalar

- AI sağlayıcısı varsayılan olarak `Mock`'tur (yerel `dotnet run` için) — RAG'a bağlı bir key/URL
  girilmeden sabit ama gerçekçi bir iş paketi listesi döner. Docker Compose ortamında `Ai__Provider=RAG`
  ile gerçek RAG servisi kullanılır.
- Kullanıcı/personel dizini yok — proje yöneticisi, onaylayıcı gibi alanlar serbest metindir (gerçek bir
  kimlik sağlayıcıya bağlanınca bu alanlar o dizinden doldurulabilir).
- Servisler arası "API Gateway yaklaşımı" tek bir YARP gateway ile karşılanmıştır; ayrı bir servis keşif
  (service discovery) mekanizması yoktur — Docker Compose DNS'i bu ölçekte yeterlidir.
- RAG / Query Expansion / on-premise LLM ince ayarı bu backend'in kapsamında değildir — proje kartında
  bunlar zaten "ön çalışma/Ar-Ge" olarak, tamamlanmış değil, planlanan olarak işaretlenmiştir.
