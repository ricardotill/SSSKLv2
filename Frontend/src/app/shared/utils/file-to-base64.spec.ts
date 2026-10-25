import { vi } from 'vitest';
import { fileToBase64 } from './file-to-base64';

describe('fileToBase64', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('returns the base64 content without the data URL prefix', async () => {
    const result = await fileToBase64(new Blob(['hello'], { type: 'text/plain' }));
    expect(result).toBe(btoa('hello'));
  });

  it('returns the result as is when it has no prefix', async () => {
    vi.stubGlobal(
      'FileReader',
      class {
        result = 'plain';
        onload?: () => void;
        readAsDataURL() {
          this.onload?.();
        }
      },
    );
    await expect(fileToBase64(new Blob(['x']))).resolves.toBe('plain');
  });

  it('rejects when the file cannot be read', async () => {
    vi.stubGlobal(
      'FileReader',
      class {
        error = null;
        onerror?: () => void;
        readAsDataURL() {
          this.onerror?.();
        }
      },
    );
    await expect(fileToBase64(new Blob(['x']))).rejects.toThrow('Failed to read file');
  });
});
