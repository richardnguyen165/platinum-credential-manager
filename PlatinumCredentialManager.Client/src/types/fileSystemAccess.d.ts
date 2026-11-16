// File System Access API (Chromium only) — not included in TypeScript's DOM lib.
// Augments the global Window so window.showSaveFilePicker typechecks.
export {};

declare global {
  interface SaveFilePickerOptions {
    suggestedName?: string;
    types?: { description?: string; accept: Record<string, string[]> }[];
  }

  interface Window {
    showSaveFilePicker(options?: SaveFilePickerOptions): Promise<FileSystemFileHandle>;
  }
}
