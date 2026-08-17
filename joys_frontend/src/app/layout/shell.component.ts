import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, map } from 'rxjs/operators';

@Component({
    selector: 'app-shell',
    templateUrl: './shell.component.html',
    styleUrls: ['./shell.component.scss'],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class ShellComponent {
    isAdmin = false;

    constructor(private router: Router) {
        this.router.events
            .pipe(
                filter((e): e is NavigationEnd => e instanceof NavigationEnd),
                map(e => e.urlAfterRedirects || e.url)
            )
            .subscribe(url => {
                this.isAdmin = url.startsWith('/admin');
            });
    }
}