import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-forbidden',
  template: `
    <div class="error-view reveal">
      <div class="container text-center">
        <span class="error-code serif">403</span>
        <h1 class="serif">Accès Réservé</h1>
        <p class="subtitle">Vous n'avez pas les autorisations nécessaires pour accéder à cet espace.</p>
        <div class="actions">
          <a routerLink="/" class="btn btn-primary">Retour à l'Accueil</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .error-view {
      min-height: 80svh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg);
    }
    .error-code {
      font-size: clamp(5rem, 15vw, 10rem);
      opacity: 0.06;
      display: block;
      margin-bottom: -40px;
      color: var(--text);
      font-family: var(--font-serif);
    }
    h1 {
      font-size: clamp(1.8rem, 4vw, 3rem);
      margin-bottom: 20px;
      color: var(--text);
    }
    .subtitle {
      color: var(--text-muted);
      max-width: 400px;
      margin: 0 auto 40px;
      line-height: 1.7;
    }
  `],
  changeDetection: ChangeDetectionStrategy.Eager,
  standalone: false
})
export class ForbiddenComponent { }
