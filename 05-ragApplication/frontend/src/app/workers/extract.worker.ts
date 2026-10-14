/// <reference lib="webworker" />

import { extractText } from '../services/extract-text';

interface ExtractRequest {
  name: string;
  type: string;
  file: File;
}

interface ExtractResponse {
  ok: boolean;
  text?: string;
  error?: string;
}

self.addEventListener('message', (event: MessageEvent<ExtractRequest>) => {
  const { file } = event.data;

  extractText(file)
    .then((text) => {
      const response: ExtractResponse = { ok: true, text };
      self.postMessage(response);
    })
    .catch((err: unknown) => {
      const response: ExtractResponse = { ok: false, error: err instanceof Error ? err.message : String(err) };
      self.postMessage(response);
    });
});