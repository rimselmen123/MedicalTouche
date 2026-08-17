import { Pipe, PipeTransform } from '@angular/core';
import { environment } from '../../../environments/environment';

@Pipe({
    name: 'premiumImg'
})
export class PremiumImgPipe implements PipeTransform {
    transform(value: string | null | undefined): string {
        if (!value) return 'assets/placeholder.png';

        // If it's already an absolute URL, return it
        if (value.startsWith('http') || value.startsWith('data:')) {
            return value;
        }

        // Standardize base URL (remove trailing /api/ or /api)
        const baseUrl = environment.apiBaseUrl.replace(/\/api\/?$/, '');

        // Ensure value starts with a single slash
        const path = value.startsWith('/') ? value : '/' + value;

        return `${baseUrl}${path}`;
    }
}
