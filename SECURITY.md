# Security Policy

## Supported versions

During pre-1.0 development, only the latest Comet release is supported.

## Reporting a vulnerability

Please report security-sensitive issues privately to the repository owner rather
than opening a public issue. Do not attach private comic archives or personal
files to public reports.

Comet does not extract ZIP entries to the filesystem while reading. It also
rejects an individual archive entry whose uncompressed size exceeds the current
512 MiB safety limit.
