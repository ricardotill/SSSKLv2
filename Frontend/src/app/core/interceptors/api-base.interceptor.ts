import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export const apiBaseInterceptor: HttpInterceptorFn = (req, next) => {
  // If we have an API URL configured and the request is to our backend api or hubs
  if (environment.apiUrl && (req.url.startsWith('/api/') || req.url.startsWith('/hubs/'))) {
    // Make sure we don't end up with double slashes if the configured apiUrl ends with a slash
    const baseUrl = environment.apiUrl.endsWith('/') ? environment.apiUrl.slice(0, -1) : environment.apiUrl;
    
    // We clone the request to prefix the new base url
    const apiReq = req.clone({ url: `${baseUrl}${req.url}` });
    return next(apiReq);
  }
  return next(req);
};
