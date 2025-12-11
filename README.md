# CloudConverter

Basit bir video işleme SaaS demosu. React (Vite) ile frontend, .NET 8 Web API ile backend, Python worker ve MySQL 8 kullanır. S3/SQS için LocalStack ile offline çalışır.

## Mimari
- `client`: Vite + React + Tailwind; pre-signed URL ile doğrudan S3'e yükler, videoları listeler.
- `api`: .NET 8 Web API + EF Core; S3 için pre-signed URL üretir, yükleme tamamlanınca veritabanına kayıt açar ve SQS'e iş kuyruğu mesajı yollar.
- `worker`: Python 3.11; SQS kuyruğunu uzun süreli dinler, videoyu indirir, ffmpeg ile thumbnail üretir, tekrar S3'e yükler ve MySQL kaydını günceller.
- `mysql`: Kalıcı volume ile MySQL 8.0.
- `localstack`: S3 ve SQS'i simüle eder.

## Ortam Değişkenleri
`.env` dosyası oluşturup aşağıdaki anahtarları ekleyin (bkz. `env.sample`):

```
MYSQL_ROOT_PASSWORD=changeme
MYSQL_DATABASE=cloudconverter
AWS_ACCESS_KEY_ID=test
AWS_SECRET_ACCESS_KEY=test
AWS_REGION=eu-west-1
AWS_SERVICE_URL=http://localstack:4566
AWS_S3_BUCKET=cloudconverter-bucket
AWS_SQS_QUEUE_URL=http://localstack:4566/000000000000/video-jobs
VITE_API_BASE_URL=http://localhost:5000
```

## Çalıştırma
1. Docker & Docker Compose kurulu olmalı.
2. `docker-compose up --build` komutunu çalıştırın.
3. Servis portları:
   - API: `http://localhost:5000/swagger`
   - Client (Vite dev): `http://localhost:5173`
   - LocalStack: `http://localhost:4566`
4. İlk açılışta bucket/queue oluşturmak için (opsiyonel, LocalStack için):
   ```bash
   aws --endpoint-url=http://localhost:4566 s3 mb s3://cloudconverter-bucket
   aws --endpoint-url=http://localhost:4566 sqs create-queue --queue-name video-jobs
   ```

## API Özet
- `POST /api/upload/init` — `{ fileName }` alır, pre-signed PUT URL ve `s3Key` döner.
- `POST /api/upload/complete` — `{ fileName, s3Key }` alır; DB'ye `Processing` kaydı açar, SQS'e `{ videoId, s3Key }` mesajı yollar.
- `GET /api/videos` — Video listesini ve durumlarını döner.

## Worker Akışı
1. SQS long polling ile mesaj bekler.
2. Mesaj gelince S3'ten videoyu indirir.
3. ffmpeg ile ilk kare thumbnail üretir.
4. Thumbnail'i S3'e yükler.
5. MySQL'deki ilgili kaydın `Status` alanını `Completed`, `ThumbnailUrl` alanını thumbnail linki olarak günceller.

## Geliştirme Notları
- .NET EF Core `Database.EnsureCreated()` kullanıyor; kalıcı şema için migration eklenebilir.
- LocalStack yerine gerçek AWS kullanılacaksa `.env` içindeki `AWS_SERVICE_URL` silinebilir/boş bırakılabilir, gerçek credential'lar kullanılmalıdır.
- Worker `ffmpeg`'i apt ile kurar; ihtiyaç halinde codec paketleri ekleyebilirsiniz.

