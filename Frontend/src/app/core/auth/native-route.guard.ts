import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { EmbeddedService, type NativeAction } from '../services/embedded.service';

/**
 * In the mobile app, routes the app implements natively (e.g. /pos) are handed to the app instead
 * of opened in the WebView. On the initial navigation the WebView still needs a page, so it falls
 * back to /events.
 */
export const nativeRouteGuard = (action: NativeAction): CanActivateFn => () => {
  const embedded = inject(EmbeddedService);
  if (!embedded.isEmbedded) {
    return true;
  }
  embedded.notifyNative(action);
  const router = inject(Router);
  return router.navigated ? false : router.parseUrl('/events');
};
