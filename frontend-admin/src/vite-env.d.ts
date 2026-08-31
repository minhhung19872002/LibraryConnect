/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the LibraryConnect API, injected at build time. */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
