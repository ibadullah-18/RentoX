FROM postgres:17-alpine
# restic: encrypted, incremental backups to S3-compatible storage (Cloudflare R2). curl: the optional "backup is alive" call.
RUN apk add --no-cache restic curl tzdata bash
COPY run-backups.sh backup.sh restore.sh /scripts-default/
RUN chmod +x /scripts-default/*.sh
ENTRYPOINT ["/bin/bash", "/scripts-default/run-backups.sh"]
