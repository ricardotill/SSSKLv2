import { describe, it, expect } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ButtonModule } from 'primeng/button';
import { Component } from '@angular/core';

@Component({
  standalone: true,
  imports: [ButtonModule],
  template: '<p-button label="Test"></p-button>'
})
class TestComponent {}

describe('PrimeNG Test', () => {
  it('should compile component with PrimeNG', async () => {
    await TestBed.configureTestingModule({
      imports: [TestComponent]
    }).compileComponents();
    const fixture = TestBed.createComponent(TestComponent);
    expect(fixture).toBeTruthy();
  });
});
