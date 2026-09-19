import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AchievementTier } from '../../../core/models/achievement.model';

interface TierMeta {
  icon: string;
  gradient: string;
  glow: string;
  ring: string;
  text: string;
}

const TIER_META: Record<AchievementTier, TierMeta> = {
  [AchievementTier.Bronze]: {
    icon: 'pi pi-shield',
    gradient: 'bg-gradient-to-br from-amber-400 via-amber-600 to-amber-800',
    glow: 'shadow-[0_0_10px_rgba(217,119,6,0.6)]',
    ring: 'ring-amber-300 dark:ring-amber-700',
    text: 'text-white'
  },
  [AchievementTier.Silver]: {
    icon: 'pi pi-star-fill',
    gradient: 'bg-gradient-to-br from-slate-100 via-slate-300 to-slate-500',
    glow: 'shadow-[0_0_10px_rgba(203,213,225,0.8)]',
    ring: 'ring-slate-100 dark:ring-slate-200',
    text: 'text-slate-900'
  },
  [AchievementTier.Gold]: {
    icon: 'pi pi-sun',
    gradient: 'bg-gradient-to-br from-yellow-300 via-yellow-500 to-amber-600',
    glow: 'shadow-[0_0_12px_rgba(245,158,11,0.7)]',
    ring: 'ring-yellow-200 dark:ring-yellow-600',
    text: 'text-yellow-950'
  },
  [AchievementTier.Platinum]: {
    icon: 'pi pi-crown',
    gradient: 'bg-gradient-to-br from-fuchsia-400 via-purple-500 to-indigo-600',
    glow: 'shadow-[0_0_14px_rgba(168,85,247,0.7)]',
    ring: 'ring-fuchsia-200 dark:ring-fuchsia-500',
    text: 'text-white'
  }
};

@Component({
  selector: 'app-tier-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (size === 'sm') {
      <span
        class="w-7 h-7 rounded-full flex items-center justify-center border-2 border-surface-0 dark:border-surface-900 ring-2 animate-tier-pulse"
        [ngClass]="[meta.gradient, meta.glow, meta.ring]"
        [title]="tier"
      >
        <i class="text-[10px]" [ngClass]="[meta.icon, meta.text]"></i>
      </span>
    } @else {
      <span
        class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-extrabold uppercase tracking-wide whitespace-nowrap ring-1"
        [ngClass]="[meta.gradient, meta.glow, meta.ring, meta.text]"
      >
        <i class="text-xs" [ngClass]="meta.icon"></i>
        {{ tier }}
      </span>
    }
  `,
  styles: [`
    @keyframes tier-pulse {
      0%, 100% { box-shadow: 0 0 0 0 rgba(255, 255, 255, 0); }
      50% { box-shadow: 0 0 8px 2px rgba(255, 255, 255, 0.35); }
    }
    .animate-tier-pulse {
      animation: tier-pulse 2.4s ease-in-out infinite;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TierBadgeComponent {
  @Input({ required: true }) tier!: AchievementTier;
  @Input() size: 'sm' | 'md' = 'md';

  get meta(): TierMeta {
    return TIER_META[this.tier];
  }
}
