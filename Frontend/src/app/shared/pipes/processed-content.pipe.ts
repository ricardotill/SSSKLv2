import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'processedContent',
  standalone: true
})
export class ProcessedContentPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';
    // Replace non-breaking spaces with normal spaces to allow native CSS word-wrapping
    return value.replace(/&nbsp;/g, ' ').replace(/&#160;/g, ' ');
  }
}
