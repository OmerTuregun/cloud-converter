import json
import logging
import os
import tempfile
import time
from pathlib import Path

import boto3
import ffmpeg
import pymysql

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
logger = logging.getLogger("worker")


def get_env(name: str, default: str | None = None) -> str:
    value = os.getenv(name, default)
    if value is None:
        raise RuntimeError(f"Missing environment variable {name}")
    return value


def get_db_connection():
    return pymysql.connect(
        host=get_env("DB_HOST", "mysql"),
        port=int(get_env("DB_PORT", "3306")),
        user=get_env("DB_USER", "root"),
        password=get_env("DB_PASSWORD", "root"),
        database=get_env("DB_NAME", "cloudconverter"),
        cursorclass=pymysql.cursors.DictCursor,
    )


def build_boto_client(service: str):
    region = get_env("AWS_REGION", "us-east-1")
    service_url = os.getenv("AWS_SERVICE_URL")
    session = boto3.Session(
        aws_access_key_id=get_env("AWS_ACCESS_KEY_ID", "test"),
        aws_secret_access_key=get_env("AWS_SECRET_ACCESS_KEY", "test"),
        region_name=region,
    )
    config_kwargs = {"region_name": region}
    if service_url:
        config_kwargs["endpoint_url"] = service_url
    return session.client(service, **config_kwargs)


def download_video(s3, bucket: str, key: str, dest: Path):
    dest.parent.mkdir(parents=True, exist_ok=True)
    s3.download_file(bucket, key, str(dest))
    logger.info("Downloaded %s to %s", key, dest)


def create_thumbnail(input_path: Path, output_path: Path):
    (
        ffmpeg
        .input(str(input_path))
        .filter("thumbnail")
        .output(str(output_path), vframes=1, format="image2")
        .overwrite_output()
        .run(quiet=True)
    )
    logger.info("Thumbnail created at %s", output_path)


def upload_file(s3, bucket: str, key: str, file_path: Path):
    # Upload thumbnail; bucket policy (not ACL) should grant read if public access is needed
    s3.upload_file(str(file_path), bucket, key)
    logger.info("Uploaded %s to s3://%s/%s", file_path, bucket, key)


def mark_video_completed(video_id: int, thumbnail_url: str):
    with get_db_connection() as conn:
        with conn.cursor() as cursor:
            cursor.execute(
                "UPDATE Videos SET Status=%s, ThumbnailUrl=%s WHERE Id=%s",
                ("Completed", thumbnail_url, video_id),
            )
        conn.commit()
    logger.info("Video %s marked completed", video_id)


def process_message(s3, message_body: str, bucket: str, service_url: str | None):
    payload = json.loads(message_body)
    video_id = payload["videoId"]
    s3_key = payload["s3Key"]

    with tempfile.TemporaryDirectory() as tmpdir:
        tmpdir_path = Path(tmpdir)
        video_path = tmpdir_path / "input_video"
        thumb_path = tmpdir_path / "thumb.jpg"

        download_video(s3, bucket, s3_key, video_path)
        create_thumbnail(video_path, thumb_path)

        thumb_key = s3_key.rsplit(".", 1)[0] + ".jpg"
        upload_file(s3, bucket, thumb_key, thumb_path)

        if service_url:
            thumbnail_url = f"{service_url}/{bucket}/{thumb_key}".replace("http://", "http://", 1)
        else:
            thumbnail_url = f"https://{bucket}.s3.amazonaws.com/{thumb_key}"

        mark_video_completed(video_id, thumbnail_url)


def main():
    queue_url = get_env("AWS_SQS_QUEUE_URL")
    bucket = get_env("AWS_S3_BUCKET")
    service_url = os.getenv("AWS_SERVICE_URL")

    sqs = build_boto_client("sqs")
    s3 = build_boto_client("s3")

    logger.info("Worker started. Listening for messages...")

    while True:
        try:
            messages = sqs.receive_message(
                QueueUrl=queue_url, WaitTimeSeconds=20, MaxNumberOfMessages=1
            ).get("Messages", [])

            if not messages:
                continue

            for msg in messages:
                receipt_handle = msg["ReceiptHandle"]
                try:
                    process_message(s3, msg["Body"], bucket, service_url)
                    sqs.delete_message(QueueUrl=queue_url, ReceiptHandle=receipt_handle)
                except Exception as exc:
                    logger.exception("Failed to process message: %s", exc)
                    time.sleep(2)
        except Exception as exc:
            logger.exception("Polling failed: %s", exc)
            time.sleep(5)


if __name__ == "__main__":
    main()

