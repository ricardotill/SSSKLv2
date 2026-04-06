import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UrlService {
  /**
   * Resolves a relative URL to an absolute API URL if it belongs to our API.
   * @param url The URL string to resolve.
   * @returns The resolved absolute URL or the original string.
   */
  resolveApiUrl(url: string | null | undefined): string | undefined {
    if (!url) return undefined;

    // If it's already an absolute URL (starts with http) or a data URI, return as is
    if (url.startsWith('http') || url.startsWith('data:')) {
      return url;
    }

    // Force relative API paths to use the absolute base URL
    if (url.startsWith('/api/')) {
      let baseUrl = environment.apiUrl;
      
      // Fallback for production if the build-time injection failed or is empty
      if (!baseUrl && typeof window !== 'undefined') {
        const hostname = window.location.hostname;
        if (hostname === 'sssklv2.scoutingwilo.nl' || hostname === 'ambitious-desert-07f83a203.azurestaticapps.net') {
          baseUrl = 'https://api.ssskl.scoutingwilo.nl';
        }
      }

      if (baseUrl) {
        const normalizedBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
        return `${normalizedBase}${url}`;
      }
    }

    return url;
  }
}
