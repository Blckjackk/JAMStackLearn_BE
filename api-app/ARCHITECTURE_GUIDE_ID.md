# Panduan Struktur Clean Layered Architecture (Bahasa Indonesia)

## 1. Tujuan Arsitektur Ini

Arsitektur ini dibuat agar project API mudah:

- Dikembangkan saat fitur bertambah
- Diuji per lapisan
- Dipelihara dalam jangka panjang
- Diubah tanpa merusak seluruh sistem

Prinsip utamanya: setiap lapisan punya tanggung jawab yang jelas.

## 2. Struktur Folder

Struktur utama pada project API:

api-app

- Controllers
- Services
  - Interfaces
- Repositories
  - Interfaces
- Models
- DTOs
  - Projects
  - Tasks
  - Users
- Database
- Program.cs

## 3. Tanggung Jawab Tiap Layer

### Models

Berisi representasi entitas domain.
Contoh: User, Project, TaskItem, Tag, TaskTag.

Yang boleh ada:

- Property entitas
- Logic domain sederhana

Yang tidak boleh ada:

- SQL query
- Logic HTTP

### DTOs

Dipakai untuk request dan response API.
Tujuannya agar model internal tidak diekspos langsung ke client.

Contoh:

- CreateProjectDto
- UpdateProjectDto
- ProjectResponseDto
- CreateTaskDto
- UpdateTaskDto
- TaskResponseDto

### Repositories

Fokus hanya untuk akses database (ADO.NET).
Berisi:

- SqlConnection
- SqlCommand
- Query SQL
- Mapping hasil query ke Model

Yang tidak boleh ada:

- Business logic
- Aturan validasi proses bisnis

### Services

Tempat business logic aplikasi.
Tugas service:

- Validasi aturan bisnis
- Memanggil repository
- Mapping Model ke DTO response
- Mengelola alur use case

Contoh aturan bisnis:

- Due date task tidak boleh di masa lalu
- Project harus ada sebelum membuat task
- Semua tag harus valid sebelum dipasang ke task

### Controllers

Lapisan paling tipis.
Tugas controller:

- Terima request HTTP
- Terima DTO dari body/route
- Panggil service
- Kembalikan response HTTP

Yang tidak boleh ada:

- Query SQL
- Business logic rumit

### Database

Berisi pengelolaan koneksi SQL Server.
Contoh: DbConnection untuk menyediakan SqlConnection dari connection string.

## 4. Alur Data Request (Contoh)

Contoh saat client membuat task:

1. Client kirim POST ke endpoint Task.
2. Controller menerima CreateTaskDto.
3. Controller memanggil TaskService.
4. TaskService menjalankan validasi business rule.
5. TaskService memanggil TaskRepository untuk insert data.
6. Jika ada tag, TaskService memanggil TagRepository untuk relasi many-to-many.
7. Service mengembalikan TaskResponseDto.
8. Controller mengirim response ke client.

## 5. Relasi Entitas

Relasi utama dalam sistem:

- User memiliki banyak Project
- Project memiliki banyak Task
- Task memiliki banyak Tag (many-to-many melalui TaskTag)

## 6. Kenapa Struktur Ini Scalable

Keuntungan saat sistem membesar:

- Mudah tambah fitur baru tanpa mengubah semua file
- Unit test bisa fokus di service
- Logic database terpusat di repository
- Endpoint tetap bersih dan konsisten
- Resiko bug berkurang karena tanggung jawab terpisah

## 7. Praktik Terbaik yang Perlu Dijaga

- Selalu pakai DTO untuk input/output API
- Jangan akses database langsung dari controller
- Jangan taruh query SQL di service
- Gunakan async untuk akses database
- Gunakan interface untuk service dan repository
- Tambahkan validasi di service, bukan di repository

## 8. Rekomendasi Urutan Belajar

Agar cepat paham, pelajari berurutan:

1. Models dan relasi entitas
2. DTO request/response
3. Repository interface dan implementasi SQL
4. Service interface dan business logic
5. Controller flow endpoint
6. Dependency Injection di Program.cs

---

Jika kamu mau, saya bisa lanjut buatkan versi lanjutan file ini berisi:

- Diagram alur request per endpoint
- Checklist review code per layer
- Template standar saat menambah fitur baru
