import { Pipe, PipeTransform, inject } from '@angular/core';
import { UrlService } from '../../core/services/url.service';

@Pipe({
  name: 'resolveApiUrl',
  standalone: true
})
export class ResolveApiUrlPipe implements PipeTransform {
  private readonly urlService = inject(UrlService);

  transform(url: string | null | undefined): string | undefined {
    return this.urlService.resolveApiUrl(url);
  }
}
