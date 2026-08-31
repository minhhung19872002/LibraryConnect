import dayjs from 'dayjs';

/**
 * Formats an ISO timestamp in the Vietnamese convention (day first, 24-hour clock) and in the
 * browser's local time. The API always sends UTC-offset timestamps, so the conversion happens here
 * rather than being baked into the payload.
 */
export function formatDateTime(value?: string | null): string | null {
  if (!value) {
    return null;
  }

  return dayjs(value).format('HH:mm:ss DD/MM/YYYY');
}

export function formatDate(value?: string | null): string | null {
  return value ? dayjs(value).format('DD/MM/YYYY') : null;
}

/** Human readable size, used by the backup screen and the digital documents module. */
export function formatBytes(bytes: number): string {
  if (bytes <= 0) {
    return '0 B';
  }

  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  const value = bytes / 1024 ** exponent;

  return `${value.toLocaleString('vi-VN', { maximumFractionDigits: exponent === 0 ? 0 : 1 })} ${units[exponent]}`;
}

/**
 * Hands a downloaded blob to the browser. Used by every "Xuất Excel", "Xuất PDF" and "Tải về"
 * action, so the object URL is always released afterwards rather than leaking per download.
 */
export function downloadFile(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');

  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  URL.revokeObjectURL(url);
}

/** Pretty-prints the jsonb snapshots stored in the audit log; falls back to the raw text. */
export function formatJson(value?: string | null): string {
  if (!value) {
    return '';
  }

  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}
