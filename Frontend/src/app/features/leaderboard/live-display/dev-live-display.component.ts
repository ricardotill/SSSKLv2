import { Component, ChangeDetectionStrategy, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { AvatarModule } from 'primeng/avatar';
import { CarouselModule } from 'primeng/carousel';
import { LeaderboardEntryDto } from '../../../core/models/leaderboard.model';
import { ProductDto } from '../../../core/models/product.model';
import { OrderDto } from '../../../core/models/order.model';
import { EventDto } from '../../../core/models/event.model';
import { AutoScrollDirective } from '../../../shared/directives/auto-scroll.directive';
import * as QRCode from 'qrcode';

interface Slide {
  type: 'product' | 'between' | 'events';
  index: number;
  product?: ProductDto;
  leaderboardTotal?: LeaderboardEntryDto[];
  leaderboard12h?: LeaderboardEntryDto[];
  latestOrders?: OrderDto[];
  meme?: string;
}

const MEMES = [
  '50bs13.jpg', '517shd.jpg', '5oblti.jpg'
];

@Component({
  selector: 'app-dev-live-display',
  standalone: true,
  imports: [CommonModule, TableModule, AvatarModule, CarouselModule, AutoScrollDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DatePipe],
  template: `
    <div class="live-display-container w-full h-full overflow-hidden text-white relative dark bg-[#09090b]">
      <!-- Background effects -->
      <div class="absolute inset-0 bg-gradient-to-br from-[#121619] via-[#1a1f24] to-[#121619]"></div>
      <div class="absolute top-0 right-0 w-[800px] h-[800px] bg-primary-500/10 rounded-full blur-[120px] mix-blend-screen pointer-events-none"></div>
      <div class="absolute bottom-0 left-0 w-[600px] h-[600px] bg-blue-500/10 rounded-full blur-[120px] mix-blend-screen pointer-events-none"></div>

      <!-- Top Left Branding -->
      <div class="absolute top-8 left-8 z-30 flex items-center gap-4 glass-panel px-6 py-4 rounded-3xl border-white/10 bg-black/40 shadow-2xl backdrop-blur-md">
        <img src="/logo.png" alt="Logo" class="w-16 h-16 object-contain drop-shadow-lg" />
        <div class="flex flex-col">
          <span class="text-3xl font-black tracking-tighter text-white uppercase italic">Dev Mode</span>
          <span class="text-xs font-bold text-primary-400 tracking-[0.2em] uppercase opacity-80">Scaling Test (24 Users)</span>
        </div>
      </div>

      @if (slides().length > 0) {
        <div class="relative z-10 w-full h-full flex flex-col">
          <div class="progress-bar-container absolute top-0 left-0 w-full h-1 z-20 overflow-hidden">
            <div class="progress-bar-fill bg-primary-500 h-full transition-all duration-100 ease-linear" [style.width.%]="progress()"></div>
          </div>
          
          <p-carousel [value]="slides()" 
                      [numVisible]="1" 
                      [numScroll]="1" 
                      [circular]="true"
                      [page]="currentPage()"
                      (onPage)="onPageChange($event)"
                      [showNavigators]="false" 
                      [showIndicators]="true"
                      styleClass="flex-1 w-full border-none h-full">
            <ng-template pTemplate="item" let-slide let-i="index">
              <div class="h-full w-full pt-6 px-12 pb-24 overflow-hidden flex flex-col">
                @if (slide.type === 'product') {
                  <div class="grid grid-cols-2 gap-8 w-full h-[calc(100vh-8rem)] overflow-hidden">
                    <!-- Left Column: Big Card -> Contains Title and All-Time Leaderboard Card -->
                    <div class="glass-panel flex flex-col h-full overflow-hidden p-8 rounded-3xl shadow-2xl min-h-0 border-white/5">
                      <h1 class="text-6xl font-black mb-8 mt-2 text-right text-transparent bg-clip-text drop-shadow-lg" style="background-image: linear-gradient(to right, #4ade80, #bbf7d0);">{{ slide.product?.name }}</h1>
                      
                      @if (slide.leaderboardTotal && slide.leaderboardTotal.length > 0) {
                        <!-- Fixed Height Card inside Left Column -->
                        <div class="glass-panel flex flex-col flex-1 overflow-hidden p-6 rounded-2xl border-white/10 bg-black/40 shadow-inner">
                          <h2 class="text-2xl font-bold mb-6 m-0 text-center text-surface-0 tracking-wide uppercase text-sm">All Time Leaderboard (Mock)</h2>
                          <div [appAutoScroll]="slide.index === currentPage()" class="flex-1 overflow-auto custom-scrollbar">
                            <p-table [value]="slide.leaderboardTotal" styleClass="p-datatable-sm custom-dark-table">
                              <ng-template pTemplate="header">
                                <tr>
                                  <th class="w-24 text-center py-4">Plek</th>
                                  <th class="py-4">Naam</th>
                                  <th class="w-40 text-center py-4">Hoeveelheid</th>
                                </tr>
                              </ng-template>
                              <ng-template pTemplate="body" let-entry let-rowIndex="rowIndex">
                                <tr [ngClass]="rowIndex % 2 === 0 ? 'bg-transparent' : 'bg-white/5'">
                                  <td class="text-center py-3 font-semibold text-surface-400">#{{ entry.position }}</td>
                                  <td class="py-3">
                                    <div class="flex items-center gap-4">
                                      <p-avatar 
                                        class="flex-shrink-0 drop-shadow-md"
                                        [image]="entry.profilePictureUrl" 
                                        [label]="!entry.profilePictureUrl ? entry.fullName.substring(0,1) : undefined" 
                                        shape="circle"
                                        size="normal">
                                      </p-avatar>
                                      <span class="font-medium text-lg">{{ entry.fullName }}</span>
                                    </div>
                                  </td>
                                  <td class="text-center py-3 font-bold text-xl text-primary-400">{{ entry.amount }}</td>
                                </tr>
                              </ng-template>
                            </p-table>
                          </div>
                        </div>
                      }
                    </div>

                    <!-- Right Column: 2 Cards (12 Hour and Latest Orders) placed on top of each other -->
                    <div class="flex flex-col gap-8 h-full overflow-hidden min-h-0">
                      
                      <!-- Top Card (12h Leaderboard) -->
                      <div class="glass-panel flex-1 overflow-hidden flex flex-col p-6 rounded-3xl shadow-2xl border-white/5">
                        @if (slide.leaderboard12h && slide.leaderboard12h.length > 0) {
                          <h2 class="text-2xl font-bold mb-6 m-0 text-center text-white tracking-wide uppercase text-sm">Leaderboard (12 uur Mock)</h2>
                          <div [appAutoScroll]="slide.index === currentPage()" class="flex-1 overflow-auto custom-scrollbar bg-black/20 rounded-xl shadow-inner border border-white/5">
                            <p-table [value]="slide.leaderboard12h" styleClass="p-datatable-sm custom-dark-table">
                              <ng-template pTemplate="header">
                                <tr>
                                  <th class="w-24 text-center py-4">Plek</th>
                                  <th class="py-4">Naam</th>
                                  <th class="w-40 text-center py-4">Hoeveelheid</th>
                                </tr>
                              </ng-template>
                              <ng-template pTemplate="body" let-entry let-rowIndex="rowIndex">
                                <tr [ngClass]="rowIndex % 2 === 0 ? 'bg-transparent' : 'bg-white/5'">
                                  <td class="text-center py-3 font-semibold text-surface-400">#{{ entry.position }}</td>
                                  <td class="py-3">
                                    <div class="flex items-center gap-4">
                                      <p-avatar 
                                        class="flex-shrink-0 drop-shadow-md"
                                        [image]="entry.profilePictureUrl" 
                                        [label]="!entry.profilePictureUrl ? entry.fullName.substring(0,1) : undefined" 
                                        shape="circle"
                                        size="normal">
                                      </p-avatar>
                                      <span class="font-medium text-lg">{{ entry.fullName }}</span>
                                    </div>
                                  </td>
                                  <td class="text-center py-3 font-bold text-xl text-primary-400">{{ entry.amount }}</td>
                                </tr>
                              </ng-template>
                            </p-table>
                          </div>
                        }
                      </div>

                      <!-- Bottom Card (Latest Orders) -->
                      <div class="glass-panel flex-1 overflow-hidden flex flex-col p-6 rounded-3xl shadow-2xl border-white/5">
                        @if (slide.latestOrders && slide.latestOrders.length > 0) {
                          <h2 class="text-2xl font-bold mb-6 m-0 text-center text-white tracking-wide uppercase text-sm">Laatste Bestellingen (Mock)</h2>
                          <div [appAutoScroll]="slide.index === currentPage()" class="flex-1 overflow-auto custom-scrollbar bg-black/20 rounded-xl shadow-inner border border-white/5">
                            <p-table [value]="slide.latestOrders" styleClass="p-datatable-sm custom-dark-table">
                              <ng-template pTemplate="header">
                                <tr>
                                  <th class="w-24 text-center py-4">Tijd</th>
                                  <th class="py-4">Naam</th>
                                  <th class="w-32 text-center py-4">Hoeveelheid</th>
                                </tr>
                              </ng-template>
                              <ng-template pTemplate="body" let-order let-rowIndex="rowIndex">
                                <tr [ngClass]="rowIndex % 2 === 0 ? 'bg-transparent' : 'bg-white/5'">
                                  <td class="text-center py-3 font-medium text-surface-400">{{ formatTime(order.createdOn) }}</td>
                                  <td class="py-3">
                                    <div class="flex items-center gap-4">
                                      <p-avatar 
                                        class="flex-shrink-0 drop-shadow-md"
                                        [image]="order.profilePictureUrl"
                                        [label]="!order.profilePictureUrl ? formatUserInitials(order.userFullName) : undefined" 
                                        shape="circle"
                                        size="normal">
                                      </p-avatar>
                                      <span class="font-medium text-lg">{{ formatUser(order.userFullName) }}</span>
                                    </div>
                                  </td>
                                  <td class="text-center py-3 font-bold text-xl text-primary-400">{{ order.amount }}</td>
                                </tr>
                              </ng-template>
                            </p-table>
                          </div>
                        }
                      </div>
                    </div>
                  </div>
                } @else if (slide.type === 'between') {
                  <!-- In-between Slide -->
                  <div class="grid grid-cols-[1fr_1fr_1fr] gap-8 flex-1 min-h-0 w-full items-center">
                    
                    <!-- Left: Meme -->
                    <div class="flex flex-col h-full justify-center overflow-hidden">
                      <div class="glass-panel p-4 rounded-3xl overflow-hidden shadow-2xl border-white/10 hover:scale-[1.02] transition-transform duration-500">
                        <div class="w-full h-96 bg-white/5 flex items-center justify-center rounded-2xl text-surface-600 font-bold italic">
                           Meme Placeholder (Scaling Test)
                        </div>
                      </div>
                    </div>

                    <!-- Middle: QR and Link -->
                    <div class="flex flex-col items-center justify-center text-center gap-12">
                       <h2 class="text-5xl font-black italic mb-4 text-white drop-shadow-xl uppercase tracking-tight">Vergeet niet te strepen!</h2>
                       <div class="qr-container-new p-12 rounded-[3.5rem] bg-white/[0.03] border border-white/10 shadow-3xl relative flex items-center justify-center">
                          @if (qrCodeUrl()) {
                            <img [src]="qrCodeUrl()" class="w-64 h-64 rounded-xl shadow-[0_0_50px_rgba(34,197,94,0.4)] transition-all duration-700 hover:scale-105" alt="QR Code" />
                          }
                       </div>
                       <p class="text-4xl font-mono text-primary-400 tracking-widest m-0 font-bold bg-black/40 px-8 py-4 rounded-full border border-primary-500/20 shadow-lg uppercase">dev.localhost</p>
                    </div>

                    <!-- Right: Agenda -->
                    <div class="flex flex-col h-full overflow-hidden">
                      <div class="glass-panel flex flex-col h-full overflow-hidden p-6 rounded-3xl border-white/10 shadow-2xl">
                        <h2 class="text-3xl font-bold mb-6 text-white flex items-center gap-4">
                          <i class="pi pi-calendar text-primary-400 text-3xl"></i>
                          Agenda (Mock)
                        </h2>
                        
                        <div [appAutoScroll]="slide.index === currentPage()" class="flex-1 flex flex-col gap-6 overflow-auto custom-scrollbar pr-4">
                          @for (event of publicEvents(); track event.id; let isFirst = $first) {
                            <div class="event-card glass-panel p-8 rounded-2xl transition-all flex flex-col gap-4"
                                 [ngClass]="isFirst ? 'bg-primary-500/5 border-primary-500/20 shadow-[0_0_30px_rgba(34,197,94,0.1)]' : 'bg-white/[0.02] border-white/5 hover:bg-white/[0.05]'">
                              <div class="flex gap-6 items-center" [ngClass]="isFirst ? 'flex-col items-start gap-4' : ''">
                                <div class="flex-1 flex flex-col gap-1 w-full">
                                  <div class="flex justify-between items-start">
                                    <span class="text-primary-400 font-bold text-sm tracking-widest uppercase">
                                      Day {{ $index + 1 }}
                                    </span>
                                  </div>
                                  <h3 class="font-bold m-0 text-white leading-tight tracking-tight"
                                      [ngClass]="isFirst ? 'text-3xl' : 'text-2xl'">{{ event.title }}</h3>
                                </div>
                              </div>
                            </div>
                          }
                        </div>
                      </div>
                    </div>

                  </div>
                }
              </div>
            </ng-template>
          </p-carousel>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100vh;
      overflow: hidden;
    }
    .glass-panel {
      background: rgba(255, 255, 255, 0.03);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.05);
      box-shadow: 0 4px 30px rgba(0, 0, 0, 0.1);
    }
    :host ::ng-deep .live-display-container.dark {
      --p-surface-0: #09090b;
      --p-surface-50: #18181b;
      --p-surface-100: #27272a;
      --p-surface-200: #3f3f46;
      --p-surface-300: #52525b;
      --p-surface-400: #71717a;
      --p-surface-500: #a1a1aa;
      --p-surface-600: #d4d4d8;
      --p-surface-700: #e4e4e7;
      --p-surface-800: #f4f4f5;
      --p-surface-900: #fafafa;
      --p-surface-950: #ffffff;
      --p-text-color: rgba(255, 255, 255, 0.95);
      --p-text-muted-color: rgba(255, 255, 255, 0.6);
      color-scheme: dark;
    }
    :host ::ng-deep .p-datatable { background: transparent !important; }
    :host ::ng-deep .p-datatable-thead > tr > th {
      background: rgba(255, 255, 255, 0.05) !important;
      color: white !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.1) !important;
    }
    :host ::ng-deep .p-datatable-tbody > tr {
      background: transparent !important;
      color: white !important;
    }
    :host ::ng-deep .p-datatable-tbody > tr:nth-child(even) { background: rgba(255, 255, 255, 0.02) !important; }
    :host ::ng-deep .p-datatable-tbody > tr > td {
      background: transparent !important;
      color: white !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.05) !important;
    }
    ::ng-deep .p-carousel,
    ::ng-deep .p-carousel-content,
    ::ng-deep .p-carousel-container,
    ::ng-deep .p-carousel-content-container,
    ::ng-deep .p-carousel-items-content,
    ::ng-deep .p-carousel-items-container {
      height: 100% !important;
      max-height: 100vh !important;
      overflow: hidden !important;
      min-height: 0 !important;
      display: flex !important;
      flex-direction: column !important;
    }
    
    ::ng-deep .p-carousel-item {
      height: 100vh !important;
      max-height: 100vh !important;
      flex: 0 0 100% !important; /* Force items to be 100% of parent width, NOT height */
      width: 100% !important;
      min-height: 0 !important;
      display: flex !important;
      flex-direction: column !important;
      justify-content: flex-start !important;
      overflow: hidden !important;
    }

    ::ng-deep .p-carousel-indicators {
      padding: 1rem !important;
      z-index: 9999 !important;
      position: fixed !important;
      bottom: 2rem !important;
      left: 0 !important;
      right: 0 !important;
      width: 100% !important;
      display: flex !important;
      justify-content: center !important;
      gap: 0.5rem !important;
      overflow: visible !important;
      pointer-events: auto !important;
    }

    ::ng-deep .p-carousel-indicator > button {
      width: 1.5rem !important;
      height: 0.6rem !important;
      background-color: rgba(255, 255, 255, 0.15) !important;
      border-radius: 999px !important;
      transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1) !important;
      border: 1px solid rgba(255, 255, 255, 0.05) !important;
      padding: 0 !important;
      display: block !important;
      cursor: pointer !important;
    }
    
    ::ng-deep .p-carousel-indicator.p-highlight > button,
    ::ng-deep .p-carousel-indicator.p-active > button,
    ::ng-deep .p-carousel-indicator[data-p-active="true"] > button {
      background-color: #22c55e !important; /* Striking green */
      width: 4rem !important;
      opacity: 1 !important;
      box-shadow: 0 0 20px rgba(34, 197, 94, 0.6);
      border-color: rgba(255, 255, 255, 0.2) !important;
    }
    .custom-dark-table .p-datatable-header, .custom-dark-table .p-datatable-thead > tr > th {
      background: transparent !important;
      color: #9ca3af !important;
      border: none !important;
      border-bottom: 2px solid rgba(255,255,255,0.05) !important;
      font-weight: 600;
      text-transform: uppercase;
      font-size: 0.875rem;
      letter-spacing: 0.05em;
    }
    .custom-dark-table .p-datatable-tbody > tr { background: transparent !important; color: #fff !important; }
    .custom-dark-table .p-datatable-tbody > tr > td { border: none !important; border-bottom: 1px solid rgba(255,255,255,0.02) !important; }
    .custom-scrollbar::-webkit-scrollbar { width: 8px; }
    .custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
    .custom-scrollbar::-webkit-scrollbar-thumb { background: rgba(255, 255, 255, 0.1); border-radius: 9999px; }
    .event-card { border-left: 4px solid #22c55e; }
  `]
})
export class DevLiveDisplayComponent implements OnInit, OnDestroy {
  slides = signal<Slide[]>([]);
  publicEvents = signal<EventDto[]>([]);
  progress = signal<number>(0);
  currentPage = signal<number>(0);
  qrCodeUrl = signal<string | null>(null);
  private progressInterval: any = null;

  ngOnInit() {
    this.generateMockData();
    this.startManualCycle();
    this.generateQRCode('https://localhost:4200/dev');
  }

  generateMockData() {
    const fakeUsers = Array.from({ length: 24 }, (_, i) => ({
      position: i + 1,
      fullName: `User ${i + 1}`,
      productName: 'Mock Product',
      amount: Math.floor(Math.random() * 100) + 1,
      profilePictureUrl: undefined
    }));

    const mockProducts: ProductDto[] = [
      { id: '1', name: 'Scaling Test: Beer', price: 1, stock: 100, enableLeaderboard: true },
      { id: '2', name: 'Scaling Test: Soda', price: 1, stock: 100, enableLeaderboard: true },
    ];

    const mockEvents: EventDto[] = Array.from({ length: 5 }, (_, i) => ({
      id: i.toString(),
      title: `Scaling Event ${i + 1}`,
      description: 'Test event',
      startDateTime: new Date().toISOString(),
      endDateTime: new Date().toISOString(),
      creatorName: 'Dev',
      createdOn: new Date().toISOString(),
      acceptedUsers: [],
      declinedUsers: []
    }));

    this.publicEvents.set(mockEvents);

    const slides: Slide[] = [];
    mockProducts.forEach((p, idx) => {
      slides.push({
        type: 'product',
        index: slides.length,
        product: p,
        leaderboardTotal: [...fakeUsers].sort((a,b) => b.amount - a.amount),
        leaderboard12h: [...fakeUsers].slice(0, 15).sort((a,b) => b.amount - a.amount),
        latestOrders: Array.from({ length: 24 }, (_, i) => ({
          id: i.toString(),
          userFullName: `User ${i + 1}`,
          userId: `u${i + 1}`,
          productName: p.name,
          productId: p.id,
          amount: 1,
          paid: 1,
          createdOn: new Date().toISOString()
        }))
      });
      
      slides.push({
        type: 'between',
        index: slides.length,
        meme: '50bs13.jpg'
      });
    });

    this.slides.set(slides);
  }

  async generateQRCode(text: string) {
    try {
      this.qrCodeUrl.set(await QRCode.toDataURL(text, { color: { dark: '#22c55e', light: '#00000000' }, margin: 1, width: 256 }));
    } catch {}
  }

  startManualCycle() {
    if (this.progressInterval) clearInterval(this.progressInterval);
    this.progress.set(0);
    this.progressInterval = setInterval(() => {
      this.progress.update(p => {
        const next = p + (100 / 300);
        if (next >= 100) { this.nextPage(); return 0; }
        return next;
      });
    }, 100);
  }

  nextPage() {
    if (this.slides().length > 0) {
      this.currentPage.set((this.currentPage() + 1) % this.slides().length);
    }
  }

  onPageChange(event: any) {
    if (event.page !== this.currentPage()) {
      this.currentPage.set(event.page);
      this.startManualCycle();
    }
  }

  ngOnDestroy() {
    if (this.progressInterval) clearInterval(this.progressInterval);
  }

  formatTime(dateStr: string) { return new Date(dateStr).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); }
  formatUser(name: string) { return name; }
  formatUserInitials(name: string) { return name.split(' ').map(n => n[0]).join(''); }
}
