import * as pdfjs from 'pdfjs-dist';
import * as XLSX from 'xlsx';
import { extractExcelStream } from './excel-stream';

// Resolve the pdf.js worker relative to the app origin so it works both on the
// main thread and inside our own Web Worker (nested-worker + fake-worker fallbacks
// both rely on workerSrc being absolute).
const origin = (globalThis as unknown as { location?: { origin?: string } }).location?.origin;
pdfjs.GlobalWorkerOptions.workerSrc = origin
  ? `${origin}/assets/pdf.worker.min.mjs`
  : 'assets/pdf.worker.min.mjs';

export async function extractText(file: File): Promise<string> {
  const lower = file.name.toLowerCase();

  let header: Uint8Array;
  try {
    header = new Uint8Array(await file.slice(0, 8).arrayBuffer());
  } catch (err) {
    throw new Error(`Could not read the file header: ${msg(err)}`);
  }

  const isPdf = lower.endsWith('.pdf')
    || file.type === 'application/pdf'
    || magicMatches(header, [0x25, 0x50, 0x44, 0x46, 0x2D]); // %PDF-

  if (isPdf) {
    try {
      return await extractPdf(file);
    } catch (err) {
      throw new Error(`PDF: ${msg(err)}`);
    }
  }

  const isExcel = lower.endsWith('.xlsx') || lower.endsWith('.xls') || lower.endsWith('.xlsm')
    || file.type === 'application/vnd.ms-excel'
    || file.type === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    || magicMatches(header, [0xd0, 0xcf, 0x11, 0xe0]); // OLE compound (legacy .xls)

  if (isExcel) {
    try {
      return await extractExcel(file);
    } catch (err) {
      throw new Error(`Spreadsheet: ${msg(err)}`);
    }
  }

  try {
    return await file.text();
  } catch (err) {
    throw new Error(`Text: ${msg(err)}`);
  }
}

async function extractPdf(file: File): Promise<string> {
  // Pass the raw bytes to pdf.js directly. We run inside a Web Worker so holding a
  // copy here is isolated from the main thread's heap (bounded by the 25 MB cap) and
  // avoids relying on Blob-URL fetching inside worker contexts.
  const data = await file.arrayBuffer();
  const loadingTask = pdfjs.getDocument({ data, isEvalSupported: false });
  try {
    const pdf = await loadingTask.promise;
    let text = '';
    for (let pageNumber = 1; pageNumber <= pdf.numPages; pageNumber++) {
      const page = await pdf.getPage(pageNumber);
      try {
        const content = await page.getTextContent();
        text += content.items
          .filter((item): item is Extract<typeof item, { str: unknown }> => 'str' in item)
          .map(item => item.str)
          .join(' ') + '\n';
      } finally {
        page.cleanup();
      }
    }

    if (!text.trim()) {
      throw new Error('No extractable text was found in this PDF (scanned images?).');
    }
    return text;
  } finally {
    try { await loadingTask.destroy(); } catch { /* ignore */ }
  }
}

async function extractExcel(file: File): Promise<string> {
  const lower = file.name.toLowerCase();
  const isOpenXml = lower.endsWith('.xlsx') || lower.endsWith('.xlsm')
    || file.type === 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

  // Always stream openxml workbooks: SheetJS (XLSX.read) inflates the whole workbook and
  // can OOM the tab even on small files with pathological content. Our streaming extractor
  // has bounded memory by construction.
  if (isOpenXml) {
    return extractExcelStream(file);
  }

  const bytes = await file.arrayBuffer();
  const workbook = XLSX.read(bytes, { type: 'array' });
  const parts: string[] = [];

  for (const sheetName of workbook.SheetNames) {
    const sheet = workbook.Sheets[sheetName];
    if (!sheet) {
      continue;
    }

    // Iterate the actual stored cells only. sheet_to_json()/!ref scans materialize the
    // whole referenced range — a stray far-away cell can push that into hundreds of
    // megabytes ("Array buffer allocation failed"). Sparse iteration is safe.
    const rows = new Map<number, Map<number, string>>();
    for (const key of Object.keys(sheet)) {
      if (key.startsWith('!')) {
        continue;
      }
      const addr = XLSX.utils.decode_cell(key);
      const cell = sheet[key];
      const value = String(typeof cell?.w === 'string' ? cell.w : cell?.v ?? '').trim();
      if (value === '') {
        continue;
      }
      if (!rows.has(addr.r)) {
        rows.set(addr.r, new Map());
      }
      rows.get(addr.r)!.set(addr.c, value);
    }

    const dataRows = [...rows.entries()]
      .sort((a, b) => a[0] - b[0]);
    if (dataRows.length === 0) {
      continue;
    }

    const headerRow = dataRows[0];
    const maxColumn = Math.max(...dataRows.map(([, cols]) => Math.max(...cols.keys())));
    parts.push(`Sheet "${sheetName}":`);

    for (const [rowIndex, cols] of dataRows) {
      if (rowIndex === headerRow[0]) {
        parts.push([...cols.entries()].sort((a, b) => a[0] - b[0]).map(([, v]) => v).join(' | '));
        continue;
      }

      const fields: string[] = [];
      for (let c = 0; c <= maxColumn; c++) {
        const value = cols.get(c) ?? '';
        const label = headerRow[1].get(c) ?? '';
        if (label && value) {
          fields.push(`${label}: ${value}`);
        } else if (value) {
          fields.push(value);
        }
      }
      if (fields.length > 0) {
        parts.push(fields.join(', '));
      }
    }
  }

  const text = parts.join('\n');
  if (!text.trim()) {
    throw new Error('No readable cells were found in this spreadsheet.');
  }
  return text;
}

function magicMatches(bytes: Uint8Array, magic: number[]): boolean {
  return bytes.length >= magic.length && magic.every((b, i) => bytes[i] === b);
}

function msg(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}