# 🖥️ Proje Yönetimi Modülü — Web (Angular Frontend)

<p>
  <img alt="Angular" src="https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white">
  <img alt="TypeScript" src="https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript&logoColor=white">
  <img alt="Docker" src="https://img.shields.io/badge/Docker-nginx-2496ED?logo=docker&logoColor=white">
</p>

Monorepo'nun Angular 21 (standalone components) tabanlı web istemcisi. Genel proje yapısı ve
backend/mobile ile ilişkisi için [kök README](../README.md)'ye bakın.

> 🔒 Bu klasörde gerçek şirket verisi/API adresi yoktur — API adresi tarayıcının o an açık olduğu
> adresten otomatik türetilir (bkz. [Ortam Değişkenleri](#-ortam-değişkenleri)), commit edilen bir
> `.env` dosyası yok.

## İçindekiler

- [Gereksinimler](#-gereksinimler)
- [Kurulum ve Çalıştırma](#-kurulum-ve-çalıştırma)
- [Ortam Değişkenleri](#-ortam-değişkenleri)
- [Testler](#-testler)

## ⚙️ Gereksinimler

- [Node.js 22+](https://nodejs.org/) ve npm

## 🚀 Kurulum ve Çalıştırma

**Geliştirme (canlı reload):**

```bash
cd web
npm install
npm start        # ng serve — http://localhost:4300
```

**Prod benzeri (Docker, nginx ile):**

```bash
cd web
docker compose up -d --build     # http://localhost:4300
```

Sunucuya deploy için `deploy/nginx.conf` ve `deploy/frontend.service` (systemd) referans alınabilir;
`frontend.service` içindeki `WorkingDirectory` yolunu kendi sunucunuza göre güncelleyin (repo'nun
`web/` alt klasörünü göstermeli).

## 🔧 Ortam Değişkenleri

`src/environments/environment.ts`, API adresini `window.location.hostname:7500` olarak tarayıcıdan
otomatik türetir — commit edilen veya elle doldurulması gereken bir `.env` dosyası yoktur.

| Değişken | Nerede | Açıklama |
|---|---|---|
| `FRONTEND_PORT` | kabuk ortamı (opsiyonel) | Docker container portu (varsayılan `4300`) |

Backend'e bağlı ortam değişkenleri (`RAG_BASE_URL`, `GATEWAY_PORT` vb.) için
[backend/.env.example](../backend/.env.example)'a bakın.

## ✅ Testler

```bash
cd web
npm test
```
