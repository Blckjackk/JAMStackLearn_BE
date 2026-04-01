# AI Playbook - API App

Dokumen ini adalah patokan saat memakai AI agar backend tetap terstruktur, konsisten, dan rapi.

## 1. Tujuan

- Menjaga struktur folder tetap jelas (Controllers/Services/Repositories/DTOs/Models).
- Mencegah perubahan liar di banyak file yang tidak perlu.
- Menyamakan gaya coding, naming, dan pola implementasi.
- Memastikan setiap perubahan punya validasi dasar (build/test ringan).

## 2. Konteks Project

- Stack: ASP.NET Core Web API, EF Core (migrations) + ADO.NET repository.
- Database: SQL Server.
- Pola utama: Controller -> Service -> Repository.
- Auth saat ini: login biasa, request yang mengubah data memakai header `X-User-Id`.

## 3. Struktur Folder Patokan

- `Controllers/` untuk API endpoints saja.
- `Services/` untuk business rules, validasi, mapping DTO.
- `Repositories/` untuk query database (ADO.NET SQL) dan mapping model.
- `DTOs/` untuk request/response shape.
- `Models/` untuk entity domain.
- `Database/` untuk DbContext, connection, seed, dan utilities.

Aturan penting:

- Jangan letakkan query SQL di Controller atau Service.
- Jangan expose model langsung ke API; gunakan DTO.
- Jangan ubah struktur folder/pola tanpa alasan kuat.
- Pastikan perubahan schema sinkron dengan repository SQL.
- Gunakan Konsep OOP, agar bisa Reusable !!

## 4. Konvensi Wajib

- C# style konsisten: PascalCase untuk class/prop, camelCase untuk local.
- Validasi input di Service layer, bukan di Controller.
- Exception handling di Controller harus konsisten (BadRequest/NotFound/Conflict/Forbidden).
- Jangan menambah dependency baru tanpa alasan jelas.
- Pastikan endpoint dan route tetap kompatibel dengan frontend.

## 5. Aturan Database & Migrations

- Semua perubahan schema harus lewat EF Core migration.
- Repository SQL wajib sesuai dengan schema yang terakhir.
- Default value di DbContext harus sama dengan default value di DB.
- Jika reset data diperbolehkan, gunakan `--db:fresh`.

## 6. Auth & Authorization

- Login menggunakan Google Authentication
- Semua action mutating (create/update/delete) pakai header `X-User-Id`.
- Role check (Project Manager, dll) tetap di Service.
- Siapkan schema untuk social auth, tetapi jangan mengubah flow login saat ini.

## 7. Alur Kerja AI (Wajib Diikuti)

Saat AI diminta mengerjakan task:

1. Analisis request dan sebut file yang akan diubah.
2. Buat rencana singkat langkah implementasi.
3. Implementasi dengan perubahan minimum yang diperlukan.
4. Jalankan validasi: `dotnet build` atau `dotnet test` bila tersedia.
5. Beri ringkasan hasil + file yang diubah + alasan teknis.

## 8. Prompt Master (Copy-Paste)

```text
Kamu adalah AI engineer untuk project backend saya.
Ikuti semua aturan di file AI.md ini.

Task: <isi task>

Batasan:
- Fokus hanya pada file yang relevan.
- Jangan ubah struktur besar tanpa alasan kuat.
- Gunakan service layer untuk business rules.
- Gunakan repository untuk query SQL.
- Jangan ubah endpoint yang dipakai frontend.

Output yang saya mau:
1) Rencana singkat
2) Daftar file yang diubah
3) Perubahan kode
4) Cara verifikasi
5) Risiko/regresi jika ada
```

## 9. Prompt Siap Pakai

### A. Tambah fitur baru

```text
Tambahkan fitur <nama fitur> sesuai aturan AI.md.
Gunakan arsitektur yang sudah ada (controllers, services, repositories, DTOs).
Jangan refactor file di luar scope fitur.
```

### B. Rapikan kode yang sudah ada

```text
Rapikan module <nama module> sesuai AI.md.
Fokus pada: validasi service, query repository, dan DTO mapping.
Jaga behavior lama tetap sama.
```

### C. Audit struktur project

```text
Audit backend project ini terhadap aturan AI.md.
Sebutkan pelanggaran prioritas tinggi dulu, lalu berikan TODO yang actionable.
```

## 10. Checklist Done

- Scope perubahan jelas dan terbatas.
- SQL repository dan schema konsisten.
- DTO dan response mapping rapi.
- Error handling terjaga.
- Validasi dasar sudah dijalankan.

## 11. Larangan

- Jangan menambah dependency baru tanpa alasan.
- Jangan mengubah naming pattern yang sudah konsisten.
- Jangan memindahkan banyak file sekaligus tanpa kebutuhan jelas.
- Jangan melakukan optimasi prematur yang menurunkan keterbacaan.

---

Jika bingung saat implementasi, pilih solusi yang paling sederhana, paling mudah dirawat, dan paling sedikit risiko regresi.
