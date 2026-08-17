import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';

@Pipe({
    name: 'fileUrl',
    standalone: false
})
export class FileUrlPipe implements PipeTransform {
    constructor(private sanitizer: DomSanitizer) { }

    transform(file: File): SafeUrl {
        if (!file) return 'assets/placeholder.png';
        const objectUrl = URL.createObjectURL(file);
        return this.sanitizer.bypassSecurityTrustUrl(objectUrl);
    }
}
