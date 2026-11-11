import { inject } from '@angular/core';
import { Router, type ActivatedRouteSnapshot, type CanActivateFn } from '@angular/router';
import { EmbeddedService, type NativeAction } from '../services/embedded.service';
import { FROM_APP_STATE } from '../services/native-bridge.service';

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

/**
 * In the mobile app, pages the app has natively (events, quotes, achievements, ...) are handed back to it
 * with their URL when the user navigates to them inside the SPA, e.g. to the event a form just saved. The
 * app then closes its WebView page and shows its own. Pages the app opened itself (the initial URL, or
 * window.ssskl.navigate) are rendered, so the app can still show them in the WebView.
 */
export const nativePageGuard: CanActivateFn = (_route, state) => {
  const embedded = inject(EmbeddedService);
  if (!embedded.isEmbedded) {
    return true;
  }
  const router = inject(Router);
  const fromApp = router.getCurrentNavigation()?.extras.state?.[FROM_APP_STATE] === true;
  if (!router.navigated || fromApp) {
    return true;
  }
  embedded.notifyNative('open', state.url);
  return false;
};
