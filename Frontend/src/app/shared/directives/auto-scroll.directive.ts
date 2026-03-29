import { Directive, ElementRef, OnInit, OnDestroy, Input, NgZone, OnChanges, SimpleChanges } from '@angular/core';

@Directive({
  selector: '[appAutoScroll]',
  standalone: true
})
export class AutoScrollDirective implements OnInit, OnDestroy, OnChanges {
  @Input('appAutoScroll') isActive: any = true;
  @Input() scrollSpeed = 30; // pixels per second
  @Input() pauseDuration = 2000; // milliseconds
  
  private animationId: number | null = null;
  private lastTimestamp = 0;
  private direction = 1; // 1 for down, -1 for up
  private isPaused = true;
  private pauseStartTime = 0;
  private internalScrollTop = 0;
  private resizeObserver: ResizeObserver | null = null;

  constructor(private el: ElementRef, private ngZone: NgZone) {}

  private get isCurrentlyActive(): boolean {
    // Treat undefined, null, or empty string (attribute only) as true
    if (this.isActive === undefined || this.isActive === null || this.isActive === '') {
      return true;
    }
    return !!this.isActive;
  }

  ngOnInit() {
    this.resetState();
    
    this.ngZone.runOutsideAngular(() => {
      this.startScrolling();
      
      this.resizeObserver = new ResizeObserver(() => {
        // Just sync internal state to native state on resize
        this.internalScrollTop = this.el.nativeElement.scrollTop;
      });
      this.resizeObserver.observe(this.el.nativeElement);
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['isActive']) {
      const prev = changes['isActive'].previousValue;
      const curr = changes['isActive'].currentValue;
      
      const wasActive = prev === undefined || prev === null || prev === '' || !!prev;
      const isNowActive = curr === undefined || curr === null || curr === '' || !!curr;
      
      if (isNowActive && !wasActive) {
        this.resetState();
      }
    }
  }

  private resetState() {
    this.internalScrollTop = 0;
    this.direction = 1;
    this.isPaused = true;
    this.pauseStartTime = 0; // Will be set in the first animation frame
    this.lastTimestamp = 0; // Will be set in the first animation frame
    
    if (this.el) {
      this.el.nativeElement.scrollTop = 0;
    }
  }

  ngOnDestroy() {
    this.stopScrolling();
    if (this.resizeObserver) {
      this.resizeObserver.disconnect();
    }
  }

  private startScrolling() {
    this.animationId = requestAnimationFrame(this.scrollStep.bind(this));
  }

  private stopScrolling() {
    if (this.animationId) {
      cancelAnimationFrame(this.animationId);
      this.animationId = null;
    }
  }

  private scrollStep(timestamp: number) {
    const element = this.el.nativeElement;
    
    // Initial/recovery state
    if (!this.lastTimestamp) {
      this.lastTimestamp = timestamp;
      if (this.isPaused && !this.pauseStartTime) {
        this.pauseStartTime = timestamp;
      }
      this.animationId = requestAnimationFrame(this.scrollStep.bind(this));
      return;
    }
    
    const deltaTime = timestamp - this.lastTimestamp;
    this.lastTimestamp = timestamp;

    const maxScroll = element.scrollHeight - element.clientHeight;

    // Do nothing if inactive OR content fits
    if (!this.isCurrentlyActive || maxScroll <= 1) { 
      if (this.internalScrollTop !== 0) {
        this.internalScrollTop = 0;
        element.scrollTop = 0;
      }
      this.animationId = requestAnimationFrame(this.scrollStep.bind(this));
      return;
    }

    if (this.isPaused) {
      // Capture start of pause if not set
      if (!this.pauseStartTime) {
        this.pauseStartTime = timestamp;
      }

      if (timestamp - this.pauseStartTime >= this.pauseDuration) {
        this.isPaused = false;
        this.pauseStartTime = 0;
        // Don't scroll this frame, wait for next clean deltaTime
      }
    } else {
      const scrollAmountPerFrame = (this.scrollSpeed * deltaTime) / 1000;
      
      // Safety cap: don't move more than 1/4 of container in one frame (prevents huge jumps on tab switch)
      const move = Math.min(scrollAmountPerFrame, element.clientHeight / 4);
      
      this.internalScrollTop += (move * this.direction);

      // Handle boundaries
      if (this.direction === 1 && this.internalScrollTop >= maxScroll) {
        this.internalScrollTop = maxScroll;
        this.direction = -1;
        this.isPaused = true;
        this.pauseStartTime = timestamp;
      } else if (this.direction === -1 && this.internalScrollTop <= 0) {
        this.internalScrollTop = 0;
        this.direction = 1;
        this.isPaused = true;
        this.pauseStartTime = timestamp;
      }
      
      element.scrollTop = this.internalScrollTop;
    }

    this.animationId = requestAnimationFrame(this.scrollStep.bind(this));
  }
}
