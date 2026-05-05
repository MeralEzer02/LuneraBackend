# The Social Match App (Frontend)

## 📌 Proje Açıklaması

Bu proje, kullanıcıların fiziksel etkenlerden bağımsız şekilde rastgele ama sistem kurallarına bağlı olarak eşleşmesini sağlayan bir platformun frontend uygulamasıdır.

Kullanıcılar:
- Kayıt olabilir
- Giriş yapabilir
- Sistem tarafından önerilen eşleşmeleri görebilir
- Eşleşmeleri kabul / reddedebilir

Backend sistem tamamen hazır bir API üzerinden çalışmaktadır.

---

## 🧠 Sistem Mantığı

Frontend sadece bir arayüzdür. Tüm iş kuralları backend’de çalışır:

- Eşleşme kuralları
- Durum yönetimi (Pending / Accepted / Rejected / Expired)
- Concurrency kontrolü
- Idempotency (aynı işlem tekrar edilse bile sistem bozulmaz)

---

## ⚙️ Kullanılan Teknolojiler

- React (Vite)
- Axios (API iletişimi)
- React Router
- Context API / (veya Redux – seçimine göre)
- TailwindCSS (opsiyonel)

---

## 📱 Sayfalar

### 1. Login Page
- Kullanıcı giriş yapar
- JWT token alınır

### 2. Register Page
- Yeni kullanıcı oluşturulur

### 3. Match List Page
- Sistem tarafından önerilen eşleşmeler listelenir

### 4. Match Actions
- Accept Match
- Reject Match
- Cancel Match

---

## 🔗 Backend Entegrasyonu

Frontend şu API yapısını kullanır:

- POST /auth/login
- POST /auth/register
- GET /matches
- POST /matches/{id}/accept
- POST /matches/{id}/reject

---

## ⚠️ Önemli Mimari Not

Frontend’de iş kuralı yoktur.

Tüm kararlar backend tarafından verilir.

Frontend sadece:
- Veri gösterir
- Kullanıcı aksiyonlarını backend’e iletir
- Response’a göre UI günceller

---

## 🚀 Kurulum

```bash
npm install
npm run dev
