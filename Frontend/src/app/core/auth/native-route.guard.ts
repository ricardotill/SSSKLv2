import { inject } from '@angular/core';
import { Router, type ActivatedRouteSnapshot, type CanActivateFn } from '@angular/router';
import { EmbeddedService, type NativeAction } from '../services/embedded.service';

/**
 * In the mobile app, routes the app implements natively (e.g. /pos) are handed to the app instead
 * of opened in the WebView. On the initial navigation the WebView still needs a page, so it falls
 * back to /events. `when` limits the hand-over to some URLs of the route (e.g. /settings without ?tab=).
 */
export const nativeRouteGuard = (
  action: NativeAction,
  when: (route: ActivatedRouteSnapshot) => boolean = () => true,
): CanActivateFn => (route) => {
  const embedded = inject(EmbeddedService);
  if (!embedded.isEmbedded || !when(route)) {
    return true;
  }
  embedded.notifyNative(action);
  const router = inject(Router);
  return router.navigated ? false : router.parseUrl('/events');
};
