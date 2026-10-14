import { Inflate } from 'fflate';

/**
 * Streaming XLSX/XLSM text extractor.
 *
 * SheetJS (XLSX.read) materialises the entire decompressed workbook in memory, which
 * blows up on large files ("Array buffer allocation failed"). This extractor instead:
 *   1. reads the zip central directory for exact per-entry byte offsets,
 *   2. stream-inflates each XML part with fflate (chunked, no full-decompression buffer),
 *   3. tokenises the XML incrementally and keeps only the cell text we actually emit.
 * Memory stays proportional to extracted text, not the workbook size.
 */

const EXTRACTED_TEXT_CAP = 40 * 1024 * 1024;
const CELL_STRING_CAP = 4000;
const SHARED_STRINGS_CAP = 32 * 1024 * 1024;

interface ZipEntry {
  name: string;
  csize: number;
  method: number;
  localHeaderOffset: number;
}

function readCentralDirectory(bytes: Uint8Array): ZipEntry[] {
  const tailStart = Math.max(0, bytes.length - (65536 + 22));
  const tail = bytes.subarray(tailStart);
  let eocd = -1;
  for (let i = tail.length - 22; i >= 0; i--) {
    if (tail[i] === 0x50 && tail[i + 1] === 0x4b && tail[i + 2] === 0x05 && tail[i + 3] === 0x06) {
      eocd = i;
      break;
    }
  }
  if (eocd === -1) {
    throw new Error('This does not look like a valid .xlsx/.xlsm file (no zip central directory).');
  }

  const centralSize = (tail[eocd + 12] | (tail[eocd + 13] << 8) | (tail[eocd + 14] << 16) | (tail[eocd + 15] << 24)) >>> 0;
  const centralOffset = (tail[eocd + 16] | (tail[eocd + 17] << 8) | (tail[eocd + 18] << 16) | (tail[eocd + 19] << 24)) >>> 0;

  const cd = bytes.subarray(centralOffset, centralOffset + centralSize);
  const entries: ZipEntry[] = [];
  let pos = 0;
  while (pos + 46 <= cd.length) {
    const sig = (cd[pos] | (cd[pos + 1] << 8) | (cd[pos + 2] << 16) | (cd[pos + 3] << 24)) >>> 0;
    if (sig !== 0x02014b50) {
      break;
    }
    const method = cd[pos + 10] | (cd[pos + 11] << 8);
    const csize = (cd[pos + 20] | (cd[pos + 21] << 8) | (cd[pos + 22] << 16) | (cd[pos + 23] << 24)) >>> 0;
    const nameLen = cd[pos + 28] | (cd[pos + 29] << 8);
    const extraLen = cd[pos + 30] | (cd[pos + 31] << 8);
    const commentLen = cd[pos + 32] | (cd[pos + 33] << 8);
    const lho = (cd[pos + 42] | (cd[pos + 43] << 8) | (cd[pos + 44] << 16) | (cd[pos + 45] << 24)) >>> 0;
    let name = '';
    for (let i = 0; i < nameLen; i++) {
      name += String.fromCharCode(cd[pos + 46 + i]);
    }
    entries.push({ name, csize, method, localHeaderOffset: lho });
    pos += 46 + nameLen + extraLen + commentLen;
  }
  return entries;
}

/** Stream-inflate one zip member (raw DEFLATE), delivering decoded UTF-8 text chunks. */
function streamEntry(
  bytes: Uint8Array,
  entry: ZipEntry,
  onChunk: (text: string, final: boolean) => void,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const lh = bytes.subarray(entry.localHeaderOffset, entry.localHeaderOffset + 30);
    const sig = (lh[0] | (lh[1] << 8) | (lh[2] << 16) | (lh[3] << 24)) >>> 0;
    if (sig !== 0x04034b50) {
      reject(new Error(`Malformed local header for ${entry.name}.`));
      return;
    }
    const nameLen = lh[26] | (lh[27] << 8);
    const extraLen = lh[28] | (lh[29] << 8);
    const dataStart = entry.localHeaderOffset + 30 + nameLen + extraLen;
    const comp = bytes.subarray(dataStart, dataStart + entry.csize);

    const decoder = new TextDecoder('utf-8');
    const emit = (u8: Uint8Array, final: boolean) => {
      const text = final ? decoder.decode(u8) : decoder.decode(u8, { stream: true });
      onChunk(text, final);
    };

    if (entry.method === 0) {
      try {
        emit(comp, true);
        resolve();
      } catch (err) {
        reject(err);
      }
      return;
    }

    try {
      const inflate = new Inflate((chunk: Uint8Array, final: boolean) => {
        emit(chunk, final);
      });
      inflate.push(comp, true);
      resolve();
    } catch (err) {
      reject(err);
    }
  });
}

/**
 * Minimal incremental XML scanner: start/end/text events.
 * Skips comments and processing instructions, routes CDATA into text,
 * handles DOCTYPE/declarations, self-closing tags and namespaced names.
 */
class XmlScanner {
  private mode:
    | 'text'
    | 'seek'
    | 'name'
    | 'endname'
    | 'attrName'
    | 'attrValue'
    | 'slash'
    | 'maybe'
    | 'comment'
    | 'cdata'
    | 'pi'
    | 'decl' = 'text';
  private textBuf = '';
  private elName = '';
  private endName = '';
  private attrName = '';
  private attrValueBuf = '';
  private attrs: Record<string, string> = {};
  private quote = '';
  private sig = '';
  private stack: string[] = [];
  private lastTwo = '';

  constructor(
    private readonly onStart: (name: string, attrs: Record<string, string>, parent: string | undefined) => void,
    private readonly onText: (text: string) => void,
    private readonly onEnd: (name: string) => void,
  ) {}

  feed(input: string): void {
    for (const ch of input) {
      switch (this.mode) {
        case 'text':
          if (ch === '<') {
            this.flushText();
            this.mode = 'seek';
          } else {
            this.textBuf += ch;
          }
          break;

        case 'seek':
          if (ch === '/') {
            this.mode = 'endname';
            this.endName = '';
          } else if (ch === '!') {
            this.sig = '<!';
            this.mode = 'maybe';
          } else if (ch === '?') {
            this.mode = 'pi';
            this.lastTwo = '';
          } else {
            this.mode = 'name';
            this.elName = ch;
            this.attrs = {};
          }
          break;

        case 'maybe':
          this.sig += ch;
          if (this.sig === '<!--') {
            this.mode = 'comment';
            this.lastTwo = '';
          } else if (this.sig === '<![CDATA[') {
            this.mode = 'cdata';
          } else if (ch === '>' || (this.sig.length >= 3 && !'<!--'.startsWith(this.sig) && !'<![CDATA['.startsWith(this.sig))) {
            this.mode = this.sig.startsWith('<?') ? 'pi' : 'decl';
            this.lastTwo = '';
          }
          break;

        case 'name':
          if (ch === '>') {
            this.mode = 'text';
            this.openElement(false);
          } else if (ch === '/') {
            this.mode = 'slash';
          } else if (ch === ' ' || ch === '\t' || ch === '\n' || ch === '\r' || ch === '\f') {
            this.mode = 'attrName';
            this.attrName = '';
          } else {
            this.elName += ch;
          }
          break;

        case 'attrName':
          if (ch === '=') {
            this.mode = 'attrValue';
            this.quote = '';
            this.attrValueBuf = '';
          } else if (ch === '>') {
            this.mode = 'text';
            this.openElement(false);
          } else if (ch === '/') {
            this.mode = 'slash';
          } else if (ch === ' ' || ch === '\t' || ch === '\n' || ch === '\r' || ch === '\f') {
            this.attrName = '';
          } else {
            this.attrName += ch;
          }
          break;

        case 'attrValue':
          if (this.quote === '') {
            if (ch === '"' || ch === "'") {
              this.quote = ch;
            }
          } else if (ch === this.quote) {
            this.attrs[this.attrName] = this.decodeEntities(this.attrValueBuf);
            this.mode = 'attrName';
            this.attrName = '';
          } else {
            this.attrValueBuf += ch;
          }
          break;

        case 'slash':
          if (ch === '>') {
            this.mode = 'text';
            this.openElement(true);
          } else {
            this.mode = 'text';
          }
          break;

        case 'endname':
          if (ch === '>') {
            this.mode = 'text';
            const name = this.endName;
            this.stack.pop();
            this.onEnd(name);
          } else {
            this.endName += ch;
          }
          break;

        case 'comment':
          if (ch === '>' && this.lastTwo === '--') {
            this.mode = 'text';
          }
          this.lastTwo = (this.lastTwo + ch).slice(-2);
          break;

        case 'cdata':
          this.textBuf += ch;
          if (this.textBuf.endsWith(']]>')) {
            this.textBuf = this.textBuf.slice(0, -3);
            this.mode = 'text';
          }
          break;

        case 'pi':
          if (ch === '>' && this.lastTwo.endsWith('?')) {
            this.mode = 'text';
          }
          this.lastTwo = (this.lastTwo + ch).slice(-2);
          break;

        case 'decl':
          if (ch === '>') {
            this.mode = 'text';
          }
          break;
      }
    }
  }

  finish(): void {
    this.flushText();
  }

  private flushText(): void {
    if (this.textBuf.length > 0) {
      this.onText(this.decodeEntities(this.textBuf));
      this.textBuf = '';
    }
  }

  private openElement(selfClosing: boolean): void {
    const localName = this.localName(this.elName);
    const parent = this.stack[this.stack.length - 1];
    if (!selfClosing) {
      this.stack.push(localName);
    }
    const decodedAttrs: Record<string, string> = {};
    for (const key of Object.keys(this.attrs)) {
      decodedAttrs[this.localName(key)] = this.attrs[key];
    }
    this.onStart(localName, decodedAttrs, parent);
    if (selfClosing) {
      this.onEnd(localName);
    }
  }

  private localName(name: string): string {
    const idx = name.indexOf(':');
    return idx === -1 ? name : name.slice(idx + 1);
  }

  private decodeEntities(s: string): string {
    if (s.indexOf('&') === -1) {
      return s;
    }
    return s.replace(/&(#\d+|#x[0-9a-fA-F]+|amp|lt|gt|quot|apos);/g, (match, entity: string) => {
      if (entity === 'amp') return '&';
      if (entity === 'lt') return '<';
      if (entity === 'gt') return '>';
      if (entity === 'quot') return '"';
      if (entity === 'apos') return "'";
      if (entity[0] === '#') {
        const radix = entity[1] === 'x' || entity[1] === 'X' ? 16 : 10;
        const code = parseInt(entity[1] === 'x' || entity[1] === 'X' ? entity.slice(2) : entity.slice(1), radix);
        return Number.isFinite(code) ? String.fromCodePoint(code) : match;
      }
      return match;
    });
  }
}

function columnIndex(letters: string): number {
  let total = 0;
  for (const ch of letters) {
    total = total * 26 + (ch.charCodeAt(0) - 64);
  }
  return total - 1;
}

function sheetNumberFromName(name: string): number {
  const match = /sheet(\d+)\.xml$/i.exec(name);
  return match ? Number(match[1]) : Infinity;
}

export async function extractExcelStream(file: File): Promise<string> {
  const bytes = new Uint8Array(await file.arrayBuffer());
  const entries = readCentralDirectory(bytes);

  // Sheet display order comes from workbook.xml.
  const wbSheetNames: string[] = [];
  const wbEntry = entries.find(e => e.name === 'xl/workbook.xml');
  if (wbEntry) {
    const scanner = new XmlScanner(
      (name, attrs) => {
        if (name === 'sheet' && typeof attrs['name'] === 'string') {
          wbSheetNames.push(attrs['name']);
        }
      },
      () => undefined,
      () => undefined,
    );
    await streamEntry(bytes, wbEntry, (text, final) => {
      scanner.feed(text);
      if (final) {
        scanner.finish();
      }
    });
  }

  // Shared string table (bounded: truncated strings, total cap).
  const sharedStrings: string[] = [];
  let sharedCharBudget = SHARED_STRINGS_CAP;
  const ssEntry = entries.find(e => e.name === 'xl/sharedStrings.xml');
  if (ssEntry) {
    let siText = '';
    let inTextElement = false;
    const scanner = new XmlScanner(
      (name) => {
        if (name === 'si') {
          siText = '';
        } else if (name === 't') {
          inTextElement = true;
        }
      },
      (text) => {
        if (inTextElement) {
          siText += text;
        }
      },
      (name) => {
        if (name === 't') {
          inTextElement = false;
        } else if (name === 'si') {
          const value = siText.trim();
          sharedStrings.push(value.length > CELL_STRING_CAP ? value.slice(0, CELL_STRING_CAP) : value);
          sharedCharBudget -= value.length;
          if (sharedCharBudget <= 0) {
            throw new Error('This workbook has an unusually large shared-string table.');
          }
        }
      },
    );
    await streamEntry(bytes, ssEntry, (text, final) => {
      scanner.feed(text);
      if (final) {
        scanner.finish();
      }
    });
  }

  const sheetEntries = entries
    .filter(e => /^xl\/worksheets\/sheet\d+\.xml$/i.test(e.name))
    .sort((a, b) => sheetNumberFromName(a.name) - sheetNumberFromName(b.name));

  if (sheetEntries.length === 0) {
    throw new Error('No worksheets were found in this workbook.');
  }

  const parts: string[] = [];
  let totalChars = 0;
  const pushSheetText = (line: string) => {
    totalChars += line.length;
    if (totalChars > EXTRACTED_TEXT_CAP) {
      throw new Error('This workbook contains too much text to index in the browser.');
    }
    parts.push(line);
  };

  for (let sheetIndex = 0; sheetIndex < sheetEntries.length; sheetIndex++) {
    const entry = sheetEntries[sheetIndex];
    const displayName = wbSheetNames[sheetIndex] ?? `Sheet ${sheetIndex + 1}`;
    pushSheetText(`Sheet "${displayName}":`);

    let header: Map<number, string> | null = null;
    let row: Map<number, string> | null = null;
    let cell: { col: number; type: string; text: string } | null = null;
    let collecting = false;

    const emitRow = (done: Map<number, string>) => {
      if (done.size === 0) {
        return;
      }
      if (!header) {
        header = done;
        const headerText = [...done.entries()]
          .sort((a, b) => a[0] - b[0])
          .map(([, v]) => v)
          .join(' | ');
        pushSheetText(headerText);
        return;
      }
      const maxCol = Math.max(...header.keys(), ...done.keys());
      const fields: string[] = [];
      for (let c = 0; c <= maxCol; c++) {
        const value = done.get(c) ?? '';
        const label = header.get(c) ?? '';
        if (label && value) {
          fields.push(`${label}: ${value}`);
        } else if (value) {
          fields.push(value);
        }
      }
      if (fields.length > 0) {
        pushSheetText(fields.join(', '));
      }
    };

    const scanner = new XmlScanner(
      (name, attrs) => {
        if (name === 'row') {
          row = new Map();
        } else if (name === 'c' && row && cell === null) {
          const ref = attrs['r'] ?? '';
          const letter = /^([A-Za-z]+)/.exec(ref)?.[1] ?? '';
          cell = {
            col: letter ? columnIndex(letter.toUpperCase()) : 0,
            type: attrs['t'] ?? '',
            text: '',
          };
        } else if (cell && (name === 'v' || name === 't')) {
          collecting = true;
        }
      },
      (text) => {
        if (collecting && cell) {
          cell.text += text;
        }
      },
      (name) => {
        if ((name === 'v' || name === 't') && cell) {
          collecting = false;
        } else if (name === 'c' && cell) {
          const t = cell.type;
          let value: string;
          if (t === 's') {
            const idx = Number(cell.text.trim());
            value = Number.isInteger(idx) && idx >= 0 ? (sharedStrings[idx] ?? '') : '';
          } else if (t === 'b') {
            value = cell.text.trim() === '1' ? 'true' : 'false';
          } else if (t === 'e') {
            value = '';
          } else {
            value = cell.text;
          }
          value = value.trim().slice(0, CELL_STRING_CAP);
          if (value !== '' && row) {
            row.set(cell.col, value);
          }
          cell = null;
        } else if (name === 'row' && row) {
          emitRow(row);
          row = null;
        }
      },
    );

    await streamEntry(bytes, entry, (text, final) => {
      scanner.feed(text);
      if (final) {
        scanner.finish();
      }
    });
  }

  const text = parts.join('\n');
  if (!text.trim()) {
    throw new Error('No readable cells were found in this spreadsheet.');
  }
  return text;
}