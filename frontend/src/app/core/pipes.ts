import { Pipe, PipeTransform } from '@angular/core';

/** "InProgress" -> "In Progress" */
@Pipe({ name: 'label', standalone: true })
export class LabelPipe implements PipeTransform {
  transform(v: string | null | undefined): string {
    return (v ?? '').replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}

/** "Feature Request" -> "featurerequest" (for css class names) */
@Pipe({ name: 'slug', standalone: true })
export class SlugPipe implements PipeTransform {
  transform(v: string | null | undefined): string {
    return (v ?? '').toLowerCase().replace(/\s+/g, '');
  }
}

/** "Rahul Verma" -> "RV" */
@Pipe({ name: 'initials', standalone: true })
export class InitialsPipe implements PipeTransform {
  transform(v: string | null | undefined): string {
    return (v ?? '?').split(' ').filter(Boolean).slice(0, 2).map((p) => p[0].toUpperCase()).join('');
  }
}
