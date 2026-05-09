# 🧠 Lunera Backend

Lunera, fiziksel faktörlere bağlı olmadan kullanıcıları **kişilik uyumu üzerinden eşleştiren** bir sistemdir.

Bu repository, sistemin **çekirdek backend mimarisini** içerir.

---

## 🚀 Amaç

Kullanıcıların:
- Rastgele ama anlamlı şekilde eşleşmesi
- Güvenli ve tutarlı bir sistemde etkileşime geçmesi
- Veri tutarlılığının %100 korunması

---

## 🏗️ Mimari

Proje modern backend prensipleriyle inşa edilmiştir:

- Clean Architecture
- CQRS Pattern
- Domain-Driven Design (DDD)
- Event-Driven Architecture
- Outbox Pattern
- Inbox Pattern

---

## ⚙️ Teknolojiler

- .NET 8
- Entity Framework Core
- SQL Server (Docker)
- MediatR
- FluentValidation
- Testcontainers

---

## 🛡️ Sistem Güvenceleri

Bu sistem **rastgele çalışan bir API değildir**.

Aşağıdaki garantiler sağlanmıştır:

- ❌ Illegal state oluşamaz
- 🔁 Idempotent işlemler (aynı istek sistemi bozmaz)
- ⚔️ Concurrency safe (race condition korumalı)
- 🔄 Transactional integrity (ya hep ya hiç)
- 📦 Event güvenliği (Outbox + Inbox)

---

## 🧪 Test Kapsamı

- Unit Tests
- Integration Tests
- Concurrency Tests
- Chaos Tests
- E2E Tests

Tüm testler:
✔ Gerçek SQL Server üzerinde  
✔ Deterministik  
✔ %100 başarılı  

---

## 🔄 Sistem Akışı

1. API request gelir
2. Command → Handler
3. Domain logic çalışır
4. Event oluşur
5. Outbox’a yazılır
6. Background worker event’i publish eder

---

## 📌 Not

Bu backend, MVP seviyesini aşan bir güvenlik ve tutarlılık seviyesine sahiptir.

---

## 👤 Geliştirici

Meral Ezer
