import { Component, ChangeDetectionStrategy, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TableModule } from 'primeng/table';
import { LeaderboardService } from '../services/leaderboard.service';
import { ProductService } from '../../pos/services/product.service';
import { OrderService } from '../../orders/services/order.service';
import { PublicService } from '../../../core/services/public.service';
import { EventService } from '../../events/services/event.service';
import { OrderDto } from '../../../core/models/order.model';
import { ProductDto } from '../../../core/models/product.model';
import { LeaderboardEntryDto } from '../../../core/models/leaderboard.model';
import { EventDto } from '../../../core/models/event.model';
import * as signalR from '@microsoft/signalr';
import confetti from 'canvas-confetti';
import { ResolveApiUrlPipe } from '../../../shared/pipes/resolve-api-url.pipe';
import { UrlService } from '../../../core/services/url.service';

interface AchievementEvent {
  achievementName: string;
  userFullName: string;
  imageUrl?: string;
}

import { AvatarModule } from 'primeng/avatar';
import { CarouselModule } from 'primeng/carousel';
import { TagModule } from 'primeng/tag';
import { switchMap, forkJoin, of, map, Observable } from 'rxjs';
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
  '50bs13.jpg', '517shd.jpg', '5oblti.jpg', '5obm28.jpg', '5obm7r.jpg',
  '5obmb5.jpg', '5obmkl.jpg', '5obmoo.jpg', '5obmqq.jpg', '5obmsv.jpg',
  '5obmvx.jpg', '5obmz5.jpg', '5obn56.jpg', '5obna4.jpg', '5obnrk.jpg',
  '5obntm.jpg', '5obnxp.jpg', '5obo2h.jpg', '5obo98.jpg', '5obobd.jpg',
  '5oboef.jpg'
];

import { AutoScrollDirective } from '../../../shared/directives/auto-scroll.directive';

@Component({
  selector: 'app-live-display',
  standalone: true,
  imports: [CommonModule, TableModule, AvatarModule, CarouselModule, TagModule, AutoScrollDirective, ResolveApiUrlPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DatePipe],
  host: {
    class: 'block w-full h-screen overflow-hidden bg-[#0a0a0c]',
    style: 'position: fixed; top: 0; left: 0; right: 0; bottom: 0; z-index: 50;'
  },
  template: `
    <div class="live-display-container w-full h-screen overflow-hidden text-white relative dark bg-[#09090b]">
      <!-- Background effects -->
      <div class="absolute inset-0 bg-gradient-to-br from-[#121619] via-[#1a1f24] to-[#121619]"></div>
      <div class="absolute top-0 right-0 w-[800px] h-[800px] bg-primary-500/10 rounded-full blur-[120px] mix-blend-screen pointer-events-none"></div>
      <div class="absolute bottom-0 left-0 w-[600px] h-[600px] bg-blue-500/10 rounded-full blur-[120px] mix-blend-screen pointer-events-none"></div>

      <!-- Top Left Branding -->
      <div class="absolute top-8 left-8 z-30 flex items-center gap-4 glass-panel px-6 py-4 rounded-3xl border-white/10 bg-black/40 shadow-2xl backdrop-blur-md">
        <img src="/logo.png" alt="Logo" class="w-16 h-16 object-contain drop-shadow-lg" />
        <div class="flex flex-col">
          <span class="text-3xl font-black tracking-tighter text-white uppercase italic">Stam stam stam...</span>
          <span class="text-xs font-bold text-primary-400 tracking-[0.2em] uppercase opacity-80">SSSKL Live Dashboard</span>
        </div>
      </div>

      @if (loading() && !product() && slides().length === 0) {
        <div class="relative z-10 flex items-center justify-center h-full">
           <i class="pi pi-spin pi-spinner text-4xl text-primary-400"></i>
        </div>
      } @else if (isCycling() && slides().length > 0) {
        <div class="relative z-10 w-full h-full flex flex-col overflow-hidden">
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
              <div class="h-screen max-h-screen w-full pt-6 px-12 pb-24 overflow-hidden flex flex-col min-h-0">
                @if (slide.type === 'product') {
                  <div class="grid grid-cols-2 gap-8 w-full h-[calc(100vh-8rem)] overflow-hidden">
                    <!-- Left Column: Big Card -> Contains Title and All-Time Leaderboard Card -->
                    <div class="glass-panel flex flex-col h-full overflow-hidden p-8 rounded-3xl shadow-2xl min-h-0 border-white/5">
                      <h1 class="text-6xl font-black mb-8 mt-2 text-right text-transparent bg-clip-text drop-shadow-lg" style="background-image: linear-gradient(to right, #4ade80, #bbf7d0);">{{ slide.product?.name }}</h1>
                      
                      @if (slide.leaderboardTotal && slide.leaderboardTotal.length > 0) {
                        <!-- Fixed Height Card inside Left Column -->
                        <div class="glass-panel flex flex-col flex-1 overflow-hidden p-6 rounded-2xl border-white/10 bg-black/40 shadow-inner">
                          <h2 class="text-2xl font-bold mb-6 m-0 text-center text-surface-0 tracking-wide uppercase text-sm">All Time Leaderboard</h2>
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
                                        [image]="entry.profilePictureUrl | resolveApiUrl" 
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
                      } @else {
                        <div class="glass-panel flex-1 flex items-center justify-center rounded-2xl border-white/10 bg-black/40">
                          <h5 class="text-xl text-surface-400 font-medium font-italic">Er is nog niets van dit product besteld.</h5>
                        </div>
                      }
                    </div>

                    <!-- Right Column: 2 Cards (12 Hour and Latest Orders) placed on top of each other -->
                    <div class="flex flex-col gap-8 h-full overflow-hidden min-h-0">
                      
                      <!-- Top Card (12h Leaderboard) -->
                      <div class="glass-panel flex-1 overflow-hidden flex flex-col p-6 rounded-3xl shadow-2xl border-white/5">
                        @if (slide.leaderboard12h && slide.leaderboard12h.length > 0) {
                          <h2 class="text-2xl font-bold mb-6 m-0 text-center text-white tracking-wide uppercase text-sm">Leaderboard (12 uur)</h2>
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
                                        [image]="entry.profilePictureUrl | resolveApiUrl" 
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
                        } @else {
                          <h5 class="text-xl text-surface-400 font-medium flex-1 flex items-center justify-center font-italic text-center">Er is de afgelopen 12 uur geen {{ slide.product?.name }} besteld.</h5>
                        }
                      </div>

                      <!-- Bottom Card (Latest Orders) -->
                      <div class="glass-panel flex-1 overflow-hidden flex flex-col p-6 rounded-3xl shadow-2xl border-white/5">
                        @if (slide.latestOrders && slide.latestOrders.length > 0) {
                          <h2 class="text-2xl font-bold mb-6 m-0 text-center text-white tracking-wide uppercase text-sm">Laatste Bestellingen</h2>
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
                                        [image]="order.profilePictureUrl | resolveApiUrl"
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
                        } @else {
                          <h5 class="text-xl text-surface-400 font-medium flex-1 flex items-center justify-center font-italic text-center">Er is de afgelopen 12 uur geen enkel product besteld.</h5>
                        }
                      </div>
                    </div>
                  </div>
                } @else {
                  <!-- In-between Slide -->
                  <div class="grid grid-cols-[1fr_1fr_1fr] gap-8 flex-1 min-h-0 w-full items-center">
                    
                    <!-- Left: Meme -->
                    <div class="flex flex-col h-full justify-center overflow-hidden">
                      <div class="glass-panel p-4 rounded-3xl overflow-hidden shadow-2xl border-white/10 hover:scale-[1.02] transition-transform duration-500">
                        <img [src]="'/images/memes/' + slide.meme" class="w-full h-full object-contain rounded-2xl" alt="Meme" />
                      </div>
                    </div>

                    <!-- Middle: QR and Link -->
                    <div class="flex flex-col items-center justify-center text-center gap-12">
                       <h2 class="text-5xl font-black italic mb-4 text-white drop-shadow-xl uppercase tracking-tight">Vergeet niet te strepen!</h2>
                       <div class="qr-container-new p-12 rounded-[3.5rem] bg-white/[0.03] border border-white/10 shadow-3xl relative flex items-center justify-center">
                          @if (qrCodeUrl()) {
                            <img [src]="qrCodeUrl()" class="w-64 h-64 rounded-xl shadow-[0_0_50px_rgba(34,197,94,0.4)] transition-all duration-700 hover:scale-105" alt="QR Code" />
                          } @else {
                            <div class="w-64 h-64 flex items-center justify-center bg-white/5 rounded-xl border border-white/10">
                              <i class="pi pi-qrcode text-6xl text-surface-600 animate-pulse"></i>
                            </div>
                          }
                       </div>
                       <p class="text-4xl font-mono text-primary-400 tracking-widest m-0 font-bold bg-black/40 px-8 py-4 rounded-full border border-primary-500/20 shadow-lg uppercase">{{ domain() }}</p>
                    </div>

                    <!-- Right: Agenda -->
                    <div class="flex flex-col h-full overflow-hidden">
                      <div class="glass-panel flex flex-col h-full overflow-hidden p-6 rounded-3xl border-white/10 shadow-2xl">
                        <h2 class="text-3xl font-bold mb-6 text-white flex items-center gap-4">
                          <i class="pi pi-calendar text-primary-400 text-3xl"></i>
                          Agenda
                        </h2>
                        
                        <div [appAutoScroll]="slide.index === currentPage()" class="flex-1 flex flex-col gap-6 overflow-auto custom-scrollbar pr-4">
                          @for (event of publicEvents(); track event.id; let isFirst = $first) {
                            <div class="event-card glass-panel p-8 rounded-2xl transition-all flex flex-col gap-4"
                                 [ngClass]="isFirst ? 'bg-primary-500/5 border-primary-500/20 shadow-[0_0_30px_rgba(34,197,94,0.1)]' : 'bg-white/[0.02] border-white/5 hover:bg-white/[0.05]'">
                              <div class="flex gap-6 items-center" [ngClass]="isFirst ? 'flex-col items-start gap-4' : ''">
                                @if (event.imageUrl) {
                                  <img [src]="event.imageUrl | resolveApiUrl" alt="Event" 
                                       class="object-cover rounded-xl shadow-lg border border-white/10 flex-shrink-0"
                                       [ngClass]="isFirst ? 'w-full h-48' : 'w-24 h-24'" />
                                }
                                <div class="flex-1 flex flex-col gap-1 w-full">
                                  <div class="flex justify-between items-start">
                                    <span class="text-primary-400 font-bold text-lg tracking-widest uppercase">
                                      {{ event.startDateTime | date:'dd MMM' }}
                                    </span>
                                    <span class="text-primary-400 text-sm font-medium">
                                      Start: {{ event.startDateTime | date:'HH:mm' }}
                                    </span>
                                  </div>
                                  <h3 class="font-bold m-0 text-white leading-tight tracking-tight"
                                      [ngClass]="isFirst ? 'text-3xl' : 'text-2xl'">{{ event.title }}</h3>
                                </div>
                              </div>
                              
                              @if (event.requiredRoles && event.requiredRoles.length > 0) {
                                <div class="flex gap-2 flex-wrap">
                                  @for (role of event.requiredRoles; track role) {
                                    <p-tag [value]="role" [rounded]="true" />
                                  }
                                </div>
                              }
                            </div>
                          } @empty {
                            <div class="flex flex-col items-center justify-center h-full text-surface-600 italic opacity-40">
                              <i class="pi pi-calendar-times text-6xl mb-6"></i>
                              <span class="text-xl">Geen geplande activiteiten</span>
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
      } @else {
        <!-- Single Product Display (Existing) -->
        <div class="relative z-10 w-full h-full flex flex-col overflow-hidden max-h-screen">
          <div class="w-full h-[calc(100vh-8rem)] mx-auto pt-8 px-12 grid grid-cols-2 gap-8 mt-4 overflow-hidden">
            
            <!-- Left Column: Big Card -> Contains Title and All-Time Leaderboard Card -->
            <div class="glass-panel flex flex-col h-full overflow-hidden p-8 rounded-3xl shadow-2xl min-h-0 border-white/5">
              @if (loading() && !product()) {
                <div class="h-16 w-96 bg-surface-800 animate-pulse rounded-lg mb-8 mx-auto mt-2"></div>
              } @else {
                <h1 class="text-6xl font-black mb-8 mt-2 text-right text-transparent bg-clip-text drop-shadow-lg" style="background-image: linear-gradient(to right, #4ade80, #bbf7d0);">{{ product()?.name }}</h1>
              }
              
              @if (leaderboardTotal().length > 0) {
                <!-- Fixed Height Card inside Left Column -->
                <div class="glass-panel flex flex-col flex-1 overflow-hidden p-6 rounded-2xl border-white/10 bg-black/40 shadow-inner">
                  <h2 class="text-2xl font-bold mb-6 m-0 text-center text-white tracking-wide uppercase text-sm">All Time Leaderboard</h2>
                  <div [appAutoScroll]="true" class="flex-1 overflow-auto custom-scrollbar">
                    <p-table [value]="leaderboardTotal()" styleClass="p-datatable-sm custom-dark-table">
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
                                [image]="entry.profilePictureUrl | resolveApiUrl" 
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
              } @else if (!loading()) {
                <div class="glass-panel flex-1 flex items-center justify-center rounded-2xl border-white/10 bg-black/40">
                  <h5 class="text-xl text-surface-400 font-medium font-italic">Er is nog niets van dit product besteld.</h5>
                </div>
              }
            </div>

            <!-- Right Column: 2 Cards (12 Hour and Latest Orders) placed on top of each other -->
            <div class="flex flex-col gap-8 h-full overflow-hidden min-h-0">
              
              <!-- Top Card (12h Leaderboard) -->
              <div class="glass-panel flex-1 overflow-hidden flex flex-col p-6 rounded-3xl shadow-2xl border-white/5">
                @if (leaderboard12h().length > 0) {
                  <h2 class="text-2xl font-bold mb-6 m-0 text-center text-white tracking-wide uppercase text-sm">Leaderboard (12 uur)</h2>
                  <div [appAutoScroll]="true" class="flex-1 overflow-auto custom-scrollbar bg-black/20 rounded-xl shadow-inner border border-white/5">
                    <p-table [value]="leaderboard12h()" styleClass="p-datatable-sm custom-dark-table">
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
                                [image]="entry.profilePictureUrl | resolveApiUrl" 
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
                } @else if (!loading()) {
                  <h5 class="text-xl text-surface-400 font-medium flex-1 flex items-center justify-center font-italic text-center">Er is de afgelopen 12 uur geen {{ product()?.name }} besteld.</h5>
                }
              </div>

              <!-- Bottom Card (Latest Orders) -->
              <div class="glass-panel flex-1 overflow-hidden flex flex-col p-6 rounded-3xl shadow-2xl border-white/5">
                @if (latestOrders().length > 0) {
                  <h2 class="text-2xl font-bold mb-6 m-0 text-center text-white tracking-wide uppercase text-sm">Laatste Bestellingen</h2>
                  <div [appAutoScroll]="true" class="flex-1 overflow-auto custom-scrollbar bg-black/20 rounded-xl shadow-inner border border-white/5">
                    <p-table [value]="latestOrders()" styleClass="p-datatable-sm custom-dark-table">
                      <ng-template pTemplate="header">
                        <tr>
                          <th class="w-24 text-center py-4">Tijd</th>
                          <th class="py-4">Naam</th>
                          <th class="py-4 text-center">Product</th>
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
                                [image]="order.profilePictureUrl | resolveApiUrl"
                                [label]="!order.profilePictureUrl ? formatUserInitials(order.userFullName) : undefined" 
                                shape="circle"
                                size="normal">
                              </p-avatar>
                              <span class="font-medium text-lg">{{ formatUser(order.userFullName) }}</span>
                            </div>
                          </td>
                          <td class="text-center py-3 text-surface-200">{{ order.productName }}</td>
                          <td class="text-center py-3 font-bold text-xl text-primary-400">{{ order.amount }}</td>
                        </tr>
                      </ng-template>
                    </p-table>
                  </div>
                } @else if (!loading()) {
                  <h5 class="text-xl text-surface-400 font-medium flex-1 flex items-center justify-center font-italic text-center">Er is de afgelopen 12 uur geen enkel product besteld.</h5>
                }
              </div>

            </div>
          </div>
        </div>
      }
    </div>

    <!-- Achievement overlay -->
    @if (showAchievementOverlay() && currentAchievementEvent()) {
      <div class="fixed inset-0 bg-black/80 backdrop-blur-md flex items-center justify-center z-[2050] pointer-events-none fade-in">
        <div class="glass-celebration text-white text-center p-16 pb-24 flex flex-col items-center gap-8 rounded-[2rem] shadow-[0_0_100px_rgba(0,0,0,0.5)] max-w-[90vw] w-[680px] pointer-events-auto pop-in border border-white/20 relative overflow-visible">
          <!-- Subtle glow effect -->
          <div class="absolute -top-10 -left-10 w-40 h-40 bg-yellow-500/10 rounded-full blur-[60px] pointer-events-none"></div>
          
          @if (currentAchievementEvent()?.imageUrl && achievementImageLoaded()) {
            <img [src]="currentAchievementEvent()?.imageUrl | resolveApiUrl" [alt]="currentAchievementEvent()?.achievementName" class="max-h-[260px] object-contain rounded-2xl mx-auto block drop-shadow-[0_20px_50px_rgba(0,0,0,0.5)] hover:scale-105 transition-transform duration-700" />
          }
          
          <div class="flex flex-col gap-3">
            <h2 class="m-0 font-black text-5xl tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-yellow-200 via-yellow-400 to-yellow-600 drop-shadow-md py-1 leading-tight">{{ currentAchievementEvent()?.achievementName }}</h2>
            <p class="opacity-90 m-0 text-3xl font-light tracking-wide">{{ currentAchievementEvent()?.userFullName }}</p>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: flex !important;
      flex-direction: column !important;
      height: 100vh !important;
      overflow: hidden !important;
    }
    .glass-panel {
      background: rgba(255, 255, 255, 0.03);
      backdrop-filter: blur(16px);
      -webkit-backdrop-filter: blur(16px);
      border: 1px solid rgba(255, 255, 255, 0.05);
      box-shadow: 0 4px 30px rgba(0, 0, 0, 0.1);
    }

    /* Force dark mode variables even if system is in light mode */
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

      --p-text-color: rgba(255, 255, 255, 0.95) !important;
      --p-text-muted-color: rgba(255, 255, 255, 0.6) !important;
      
      color-scheme: dark;
    }

    /* Force Table Dark Mode */
    :host ::ng-deep .p-datatable {
      background: transparent !important;
    }

    :host ::ng-deep .p-datatable-thead > tr > th {
      background: rgba(255, 255, 255, 0.05) !important;
      color: white !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.1) !important;
    }

    :host ::ng-deep .p-datatable-tbody > tr {
      background: transparent !important;
      color: white !important;
    }

    :host ::ng-deep .p-datatable-tbody > tr:nth-child(even) {
      background: rgba(255, 255, 255, 0.02) !important;
    }

    :host ::ng-deep .p-datatable-tbody > tr > td {
      background: transparent !important;
      color: white !important;
      border-bottom: 1px solid rgba(255, 255, 255, 0.05) !important;
    }

    /* Force Carousel Full Width */
    :host ::ng-deep .p-carousel-content,
    :host ::ng-deep .p-carousel-container {
      max-width: none !important;
      width: 100% !important;
    }

    :host ::ng-deep .live-display-container h1,
    :host ::ng-deep .live-display-container h2,
    :host ::ng-deep .live-display-container h3,
    :host ::ng-deep .live-display-container h4,
    :host ::ng-deep .live-display-container h5 {
      color: white !important;
    }
    
    .glass-celebration {
      background: linear-gradient(135deg, rgba(255, 255, 255, 0.1), rgba(255, 255, 255, 0.02));
      backdrop-filter: blur(30px);
      -webkit-backdrop-filter: blur(30px);
      box-shadow: 0 8px 32px 0 rgba(0, 0, 0, 0.37);
      min-width: 450px;
    }

    .qr-code-mask {
       width: 16rem;
       height: 16rem;
       background-color: var(--primary-500);
       mask: url('/images/link-qrcode.svg') no-repeat center;
       -webkit-mask: url('/images/link-qrcode.svg') no-repeat center;
       mask-size: contain;
       -webkit-mask-size: contain;
    }

    .custom-dark-table .p-datatable-header,
    .custom-dark-table .p-datatable-thead > tr > th {
      background: transparent !important;
      color: #9ca3af !important; /* text-surface-400 equivalent */
      border: none !important;
      border-bottom: 2px solid rgba(255,255,255,0.05) !important;
      font-weight: 600;
      text-transform: uppercase;
      font-size: 0.875rem;
      letter-spacing: 0.05em;
    }
    
    .custom-dark-table .p-datatable-tbody > tr {
      background: transparent !important;
      color: #fff !important;
      transition: background-color 0.2s ease;
    }

    .custom-dark-table .p-datatable-tbody > tr:hover {
      background-color: rgba(255, 255, 255, 0.08) !important;
    }

    .custom-dark-table .p-datatable-tbody > tr > td {
      border: none !important;
      border-bottom: 1px solid rgba(255,255,255,0.02) !important;
      background: transparent !important;
    }

    .custom-scrollbar::-webkit-scrollbar {
      width: 8px;
    }
    .custom-scrollbar::-webkit-scrollbar-track {
      background: transparent; 
    }
    .custom-scrollbar::-webkit-scrollbar-thumb {
      background: rgba(255, 255, 255, 0.1); 
      border-radius: 9999px;
    }
    .custom-scrollbar::-webkit-scrollbar-thumb:hover {
      background: rgba(255, 255, 255, 0.2); 
    }

    .fade-in {
      animation: fadeIn 0.8s ease-out forwards;
    }
    .pop-in {
      transform-origin: center;
      animation: popIn 600ms cubic-bezier(0.34, 1.56, 0.64, 1) forwards;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes popIn {
      from { opacity: 0; transform: translateY(40px) scale(0.85) rotate(-1deg); }
      to { opacity: 1; transform: translateY(0) scale(1) rotate(0); }
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

    .event-card {
      border-left: 4px solid var(--primary-500);
    }
  `]
})
export class LiveDisplayComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private leaderboardService = inject(LeaderboardService);
  private productService = inject(ProductService);
  private orderService = inject(OrderService);
  private eventService = inject(EventService);
  private publicService = inject(PublicService);
  private datePipe = inject(DatePipe);
  private urlService = inject(UrlService);

  productId = signal<string | null>(null);
  product = signal<ProductDto | null>(null);
  leaderboardTotal = signal<LeaderboardEntryDto[]>([]);
  leaderboard12h = signal<LeaderboardEntryDto[]>([]);
  latestOrders = signal<OrderDto[]>([]);

  // Cycling mode signals
  slides = signal<Slide[]>([]);
  publicEvents = signal<EventDto[]>([]);
  domain = signal<string>('ssskl.scoutingwilo.nl');
  isCycling = signal<boolean>(false);
  currentRole = signal<string | undefined>(undefined);

  loading = signal<boolean>(false);

  showAchievementOverlay = signal<boolean>(false);
  currentAchievementEvent = signal<AchievementEvent | null>(null);
  achievementImageLoaded = signal<boolean>(false);

  private hubConnection: signalR.HubConnection | null = null;
  private achievementTimeout: any = null;
  private confettiInterval: any = null;

  progress = signal<number>(0);
  currentPage = signal<number>(0);
  qrCodeUrl = signal<string | null>(null);
  private progressInterval: any = null;

  async generateQRCode(text: string) {
    if (!text) return;
    try {
      const url = await QRCode.toDataURL(text, {
        color: {
          dark: '#22c55e', // Primary Green
          light: '#00000000' // Transparent
        },
        margin: 1,
        width: 256
      });
      this.qrCodeUrl.set(url);
    } catch (err) {
      console.error('Error generating QR code', err);
    }
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.productId.set(id);
      this.loadData();
    } else {
      this.isCycling.set(true);

      // Subscribe to role query parameter for reactivity
      this.route.queryParamMap.subscribe(params => {
        const role = params.get('role') || undefined;
        this.currentRole.set(role);
        this.loadCyclingData();
      });

      this.startManualCycle();
    }
    this.initSignalR();
  }

  startManualCycle() {
    if (this.progressInterval) clearInterval(this.progressInterval);
    this.progress.set(0);

    this.progressInterval = setInterval(() => {
      this.progress.update(p => {
        const next = p + (100 / 300); // 30s = 300 steps of 100ms
        if (next >= 100) {
          this.nextPage();
          return 0;
        }
        return next;
      });
    }, 100);
  }

  nextPage() {
    if (this.slides().length > 0) {
      const p = this.currentPage();
      const next = (p + 1) % this.slides().length;
      if (next === 0) {
        this.refreshMemes();
      }
      this.currentPage.set(next);
    }
  }

  onPageChange(event: any) {
    if (event.page !== this.currentPage()) {
      this.currentPage.set(event.page);
      this.startManualCycle(); // Reset progress on manual interaction
    }
  }

  ngOnDestroy() {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
    }
    this.stopConfetti();
    if (this.achievementTimeout) {
      clearTimeout(this.achievementTimeout);
    }
  }

  loadData() {
    const id = this.productId();
    if (!id) return;

    this.loading.set(true);

    this.productService.getProduct(id).subscribe({
      next: (prod) => this.product.set(prod),
      error: () => this.loading.set(false)
    });

    this.leaderboardService.getLeaderboard(id).subscribe(res => this.leaderboardTotal.set(res));
    this.leaderboardService.get12HourLiveLeaderboard(id).subscribe(res => this.leaderboard12h.set(res));
    this.orderService.getLatestOrders().subscribe({
      next: (res) => {
        this.latestOrders.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  loadCyclingData() {
    this.loading.set(true);
    const role = this.currentRole();

    forkJoin({
      products: this.productService.getProducts(0, 100, true),
      events: this.eventService.getPublicEvents(0, 5, true, role),
      domain: this.publicService.getDomain()
    }).pipe(
      switchMap(data => {
        const pResponse = data.products as any;
        const productList = (Array.isArray(pResponse) ? pResponse : pResponse.items)
          .filter((p: any) => p.enableLeaderboard !== false);
        this.publicEvents.set(data.events.items);
        this.domain.set(data.domain);
        if (data.domain) {
          this.generateQRCode('https://' + data.domain);
        }

        const leaderboardRequests: Observable<Slide>[] = productList.map((p: any) =>
          forkJoin({
            total: this.leaderboardService.getLeaderboard(p.id!),
            h12: this.leaderboardService.get12HourLiveLeaderboard(p.id!),
            orders: this.orderService.getLatestOrders()
          }).pipe(
            map(res => ({
              type: 'product' as const,
              product: p,
              leaderboardTotal: res.total,
              leaderboard12h: res.h12,
              latestOrders: res.orders.filter(o => o.productId === p.id)
            }))
          )
        );

        if (leaderboardRequests.length === 0) return of([] as Slide[]);
        return forkJoin(leaderboardRequests);
      })
    ).subscribe({
      next: (productSlides: Slide[]) => {
        const finalSlides: Slide[] = [];
        productSlides.forEach((ps) => {
          finalSlides.push({ ...ps, index: finalSlides.length });
          finalSlides.push({
            type: 'between',
            meme: this.getRandomMeme(),
            index: finalSlides.length
          } as any);
        });
        this.slides.set(finalSlides);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  getRandomMeme(): string {
    return MEMES[Math.floor(Math.random() * MEMES.length)];
  }

  refreshMemes() {
    this.slides.update(currentSlides =>
      currentSlides.map(s => {
        if (s.type === 'between') {
          return { ...s, meme: this.getRandomMeme() };
        }
        return s;
      })
    );
  }

  private initSignalR() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/livemetrics')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().catch(err => console.error('SignalR connection error: ', err));

    this.hubConnection.on('UserPurchase', () => {
      if (this.isCycling()) {
        this.loadCyclingData();
      } else {
        this.loadData();
      }
    });

    this.hubConnection.on('Achievement', (dto: AchievementEvent) => {
      this.celebrateAchievement(dto);
      if (this.isCycling()) {
        this.loadCyclingData();
      } else {
        this.loadData();
      }
    });

    this.hubConnection.on('EventChanged', () => {
      if (this.isCycling()) {
        this.loadCyclingData();
      }
    });
  }

  private celebrateAchievement(event: AchievementEvent) {
    if (this.achievementTimeout) {
      clearTimeout(this.achievementTimeout);
    }

    this.stopConfetti();

    this.currentAchievementEvent.set(event);
    this.showAchievementOverlay.set(true);
    this.achievementImageLoaded.set(false);

    this.startConfetti();

    if (event.imageUrl) {
      const img = new Image();
      img.onload = () => this.achievementImageLoaded.set(true);
      img.src = this.urlService.resolveApiUrl(event.imageUrl) || '';
    }

    this.achievementTimeout = setTimeout(() => {
      this.showAchievementOverlay.set(false);
      this.currentAchievementEvent.set(null);
      this.stopConfetti();
    }, 5000);
  }

  private startConfetti() {
    const duration = 5000;
    const end = Date.now() + duration;

    const frame = () => {
      confetti({
        particleCount: 5,
        angle: 60,
        spread: 55,
        origin: { x: 0 },
        colors: ['#ffffff', '#ff0000', '#00ff00', '#0000ff']
      });
      confetti({
        particleCount: 5,
        angle: 120,
        spread: 55,
        origin: { x: 1 },
        colors: ['#ffffff', '#ff0000', '#00ff00', '#0000ff']
      });

      if (Date.now() < end) {
        this.confettiInterval = requestAnimationFrame(frame);
      }
    };

    frame();
  }

  private stopConfetti() {
    if (this.confettiInterval) {
      cancelAnimationFrame(this.confettiInterval);
      this.confettiInterval = null;
    }
  }

  formatTime(dateString: string | Date | undefined): string {
    if (!dateString) return '';
    return this.datePipe.transform(dateString, 'HH:mm') || '';
  }

  formatUser(fullName: string | undefined): string {
    if (!fullName) return '';
    const parts = fullName.trim().split(' ');
    if (parts.length === 1) return parts[0];

    const firstNames = parts.slice(0, -1).join(' ');
    const lastName = parts[parts.length - 1];
    return `${firstNames} ${lastName.charAt(0)}`;
  }

  formatUserInitials(fullName: string | undefined): string {
    if (!fullName) return '';
    const parts = fullName.trim().split(' ');
    if (parts.length === 1) return parts[0].substring(0, 1).toUpperCase();
    return (parts[0].substring(0, 1) + parts[parts.length - 1].substring(0, 1)).toUpperCase();
  }
}
