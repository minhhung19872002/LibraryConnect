import type { FormInstance } from 'antd';
import { ApiRequestError } from '@/api/client';
import { messages } from '@/i18n/messages';

/**
 * Moves the field errors returned by the backend onto the matching form inputs and returns the
 * message to show in a toast.
 *
 * Every form in the product handles a failed submit the same way, so the mapping lives here rather
 * than being repeated in each screen. Field names arrive camel-cased, matching the JSON payload the
 * form posts, so they line up with the Ant Design field names directly.
 */
export function applyApiError<TValues>(form: FormInstance<TValues>, error: unknown): string {
  if (!(error instanceof ApiRequestError)) {
    return messages.errors.unexpected;
  }

  const fields = Object.entries(error.fieldErrors).map(([name, errors]) => ({
    // The backend addresses nested fields with dots ("profile.fullName"); Ant Design uses a path array.
    name: name.includes('.') ? name.split('.') : name,
    errors,
  }));

  if (fields.length > 0) {
    form.setFields(fields as Parameters<FormInstance<TValues>['setFields']>[0]);
  }

  return error.message;
}

/** Message of any thrown value, for the cases where no form is involved. */
export function errorMessage(error: unknown): string {
  return error instanceof ApiRequestError ? error.message : messages.errors.unexpected;
}
