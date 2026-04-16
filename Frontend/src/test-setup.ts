import { vi, expect } from 'vitest';

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), 
    removeListener: vi.fn(), 
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

expect.addSnapshotSerializer({
  test(val) {
    return (
      val &&
      (typeof val === 'string' || (typeof val === 'object' && val.nodeType === 1)) &&
      (typeof val === 'string' ? val.includes('<') : true)
    );
  },
  print(val) {
    const html = typeof val === 'string' ? val : (val as HTMLElement).outerHTML;
    const scrubbed = html
      .replace(/id=["']root\d+["']/g, 'id="root-ID"')
      .replace(/id=root\d+/g, 'id=root-ID')
      .replace(/pn_id_\d+/g, 'pn_id_ID')
      .replace(/pc\d+=(["'])(?:(?!\1).)*\1/g, 'pc-ID=""')
      .replace(/pc\d+=""/g, 'pc-ID=""')
      .replace(/pc\d+/g, 'pc-ID')
      .replace(/_ngcontent-a-c\d+/g, '_ngcontent-ID')
      .replace(/_nghost-a-c\d+/g, '_nghost-ID')
      .replace(/_ngcontent-[^= "'>]+/g, '_ngcontent-ID')
      .replace(/_nghost-[^= "'>]+/g, '_nghost-ID');
    return scrubbed;
  },
});
