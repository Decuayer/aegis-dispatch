#!/bin/sh
set -e

echo "Waiting for MinIO server to accept connections..."
until /usr/bin/mc alias set localminio http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"; do
    sleep 2
done

echo "Creating default storage buckets..."
/usr/bin/mc mb --ignore-existing localminio/"${MINIO_DEFAULT_BUCKET:-socar-dispatch-media}"
/usr/bin/mc anonymous set download localminio/"${MINIO_DEFAULT_BUCKET:-socar-dispatch-media}"

/usr/bin/mc mb --ignore-existing localminio/socar-dispatch-incidents
/usr/bin/mc anonymous set download localminio/socar-dispatch-incidents

/usr/bin/mc mb --ignore-existing localminio/socar-dispatch-feedbacks
/usr/bin/mc anonymous set download localminio/socar-dispatch-feedbacks

echo "MinIO buckets provisioned successfully."
exit 0
    