import { TestBed } from '@angular/core/testing';
import { LanguageService } from './language.service';

describe('LanguageService', () => {
  let service: LanguageService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LanguageService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have Dutch as default language', () => {
    expect(service.language()).toBe('nl');
  });

  it('should translate correctly without params', () => {
    expect(service.translate('success')).toBe('Succes');
  });

  it('should translate correctly with params', () => {
    // We need a key that has params, e.g., 'confirm_delete_product': 'Weet je zeker dat je product {name} wilt verwijderen?'
    const translated = service.translate('confirm_delete_product', { name: 'Appel' });
    expect(translated).toContain('Appel');
    expect(translated).toBe('Weet je zeker dat je product Appel wilt verwijderen? Dit kan niet ongedaan worden gemaakt.');
  });

  it('should return key if translation is missing', () => {
    // @ts-expect-error - testing invalid key
    expect(service.translate('non_existent_key')).toBe('non_existent_key');
  });

  it('should update computed translation signal when language changes', () => {
    const t = service.t();
    expect(t['success']).toBe('Succes');
    
    // Changing language (even though current implementation only has Dutch translations)
    service.language.set('en');
    expect(service.language()).toBe('en');
    // Currently LanguageService only has dutchTranslations, so it still returns them
    expect(service.t()['success']).toBe('Succes');
  });
});
