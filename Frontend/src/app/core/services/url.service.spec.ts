import { UrlService } from './url.service';
import { environment } from '../../../environments/environment';

describe('UrlService', () => {
  const service = new UrlService();
  const originalApiUrl = environment.apiUrl;

  afterEach(() => {
    (environment as any).apiUrl = originalApiUrl;
  });

  it('returns undefined for empty values', () => {
    expect(service.resolveApiUrl(undefined)).toBeUndefined();
    expect(service.resolveApiUrl(null)).toBeUndefined();
    expect(service.resolveApiUrl('')).toBeUndefined();
  });

  it('keeps absolute, data and non-API URLs', () => {
    expect(service.resolveApiUrl('https://example.com/a.png')).toBe('https://example.com/a.png');
    expect(service.resolveApiUrl('data:image/png;base64,AAA')).toBe('data:image/png;base64,AAA');
    expect(service.resolveApiUrl('/assets/logo.png')).toBe('/assets/logo.png');
  });

  it('prefixes API paths with the API base URL', () => {
    (environment as any).apiUrl = 'https://api.example.com/';
    expect(service.resolveApiUrl('/api/v1/blob/1')).toBe('https://api.example.com/api/v1/blob/1');

    (environment as any).apiUrl = 'https://api.example.com';
    expect(service.resolveApiUrl('/api/v1/blob/1')).toBe('https://api.example.com/api/v1/blob/1');
  });

  it('keeps API paths relative without a base URL outside production hosts', () => {
    (environment as any).apiUrl = '';
    expect(service.resolveApiUrl('/api/v1/blob/1')).toBe('/api/v1/blob/1');
  });
});
