import { Component, Input, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class EmptyStateComponent {
  @Input() emoji = '🍰';
  @Input() title = 'Nothing here yet';
  @Input() description?: string;
}