import { Component, ChangeDetectionStrategy, inject, OnInit, effect, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, FormGroup } from '@angular/forms';
import { AuthService, InfoResponse, TwoFactorResponse } from '../../core/auth/auth.service';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { TabsModule } from 'primeng/tabs';
import { SelectButtonModule } from 'primeng/selectbutton';
import { FormsModule } from '@angular/forms';
import { ThemeService, ThemeMode } from '../../core/services/theme.service';
import { LanguageService } from '../../core/services/language.service';
import * as QRCode from 'qrcode';

import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, ButtonModule, InputTextModule, ToastModule, TagModule, TabsModule, ConfirmDialogModule, SelectButtonModule, CardModule],
  providers: [MessageService, ConfirmationService],
  template: `
    <div class="max-w-3xl mx-auto mt-8">
      <h1 class="text-2xl font-bold mb-4 text-surface-900 dark:text-surface-0">{{ ls.t().user_settings }}</h1>
      <p-card>
      <p-toast></p-toast>

      <p-tabs value="profile">
        <p-tablist>
            <p-tab value="profile">{{ ls.t().profile }}</p-tab>
            <p-tab value="security">{{ ls.t().security }}</p-tab>
            <p-tab value="twofactor">{{ ls.t().tfa }}</p-tab>
            <p-tab value="personaldata">{{ ls.t().personal_data }}</p-tab>
        </p-tablist>
        
        <p-tabpanels>
            <!-- Profile Tab -->
            <p-tabpanel value="profile">
              <div class="mt-4">
                @if (authService.currentUser(); as user) {
                  <div class="mb-8">
                    <h3 class="text-xl font-semibold mb-3">{{ ls.t().roles }}</h3>
                    <div class="flex gap-2 flex-wrap">
                      @for (role of user.roles; track role) {
                        <p-tag [value]="role" [severity]="getRoleSeverity(role)"></p-tag>
                      }
                      @if (!user.roles || user.roles.length === 0) {
                        <span class="text-surface-500 text-sm">{{ ls.t().no_roles }}</span>
                      }
                    </div>
                  </div>
                }
                
                <div class="mb-8 p-4 bg-surface-50 dark:bg-surface-800/50 rounded-lg border border-surface-200 dark:border-surface-700">
                  <h3 class="text-xl font-semibold mb-4 flex items-center gap-2">
                    <i class="pi pi-palette text-primary"></i>
                    {{ ls.t().color_mode }}
                  </h3>
                  <p-selectbutton 
                    [options]="themeOptions" 
                    [ngModel]="themeService.mode()" 
                    (ngModelChange)="themeService.setMode($event)" 
                    [allowEmpty]="false">
                    <ng-template #item let-item>
                        <div class="flex items-center gap-2 px-2">
                            <i [class]="item.icon"></i>
                            <span>{{item.label}}</span>
                        </div>
                    </ng-template>
                  </p-selectbutton>
                  <p class="text-sm text-surface-500 mt-3">
                    {{ ls.t().color_mode_desc }}
                  </p>
                </div>

                <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-5">
                  <div class="flex flex-col gap-2">
                    <label for="name" class="font-semibold cursor-pointer">{{ ls.t().first_name }}</label>
                    <input pInputText id="name" formControlName="name" class="w-full text-lg p-3" />
                  </div>

                  <div class="flex flex-col gap-2">
                    <label for="surname" class="font-semibold cursor-pointer">{{ ls.t().last_name }}</label>
                    <input pInputText id="surname" formControlName="surname" class="w-full text-lg p-3" />
                  </div>

                  <div class="flex flex-col gap-2">
                    <label for="phoneNumber" class="font-semibold cursor-pointer">{{ ls.t().phone_number }}</label>
                    <input pInputText id="phoneNumber" formControlName="phoneNumber" class="w-full text-lg p-3" />
                  </div>

                  <div class="flex justify-end mt-4">
                    <p-button 
                      type="submit" 
                      [label]="ls.t().save_changes" 
                      [loading]="isLoading()" 
                      [disabled]="form.invalid || form.pristine || isLoading()"
                      icon="pi pi-check"
                    ></p-button>
                  </div>
                </form>
              </div>
            </p-tabpanel>

            <!-- Account Security Tab -->
            <p-tabpanel value="security">
               <div class="flex flex-col gap-6 mt-4">
                 @if (identityInfo(); as info) {
                    <div class="flex flex-col gap-2 p-4 bg-surface-100 dark:bg-surface-800 rounded-lg">
                       <h3 class="font-semibold text-lg hover:text-primary transition-colors cursor-pointer">{{ ls.t().email_status }}</h3>
                       <div class="flex items-center gap-3">
                         <span class="text-surface-700 dark:text-surface-300 font-medium">{{ info.email }}</span>
                         @if (info.isEmailConfirmed) {
                           <p-tag severity="success" [value]="ls.t().confirmed"></p-tag>
                         } @else {
                           <p-tag severity="warn" [value]="ls.t().unconfirmed"></p-tag>
                         }
                       </div>
                    </div>
                 } @else {
                    <div class="flex justify-center p-4">
                       <i class="pi pi-spin pi-spinner text-2xl text-surface-500"></i>
                    </div>
                 }

                 <form [formGroup]="securityForm" (ngSubmit)="onSecuritySubmit()" class="flex flex-col gap-5">
                   <div class="flex flex-col gap-2">
                     <label for="newEmail" class="font-semibold cursor-pointer">{{ ls.t().new_email }}</label>
                     <input pInputText id="newEmail" type="email" formControlName="newEmail" class="w-full text-lg p-3" [placeholder]="ls.t().email_placeholder" />
                   </div>
                   
                   <div class="flex flex-col gap-2 mt-4">
                     <label for="oldPassword" class="font-semibold cursor-pointer">{{ ls.t().current_password }}</label>
                     <input pInputText id="oldPassword" type="password" formControlName="oldPassword" class="w-full text-lg p-3" [placeholder]="ls.t().password_placeholder" />
                   </div>
                   
                   <div class="flex flex-col gap-2">
                     <label for="newPassword" class="font-semibold cursor-pointer">{{ ls.t().new_password }}</label>
                     <input pInputText id="newPassword" type="password" formControlName="newPassword" class="w-full text-lg p-3" [placeholder]="ls.t().new_password_placeholder" />
                   </div>
                   
                   <div class="flex justify-end mt-4">
                     <p-button 
                       type="submit" 
                       [label]="ls.t().update_security" 
                       [loading]="isSecurityLoading()" 
                       [disabled]="securityForm.invalid || isSecurityLoading() || !isSecurityFormValid()"
                       icon="pi pi-lock"
                     ></p-button>
                   </div>
                  </form>
               </div>
            </p-tabpanel>

            <!-- 2FA Tab -->
            <p-tabpanel value="twofactor">
               <div class="mt-4 flex flex-col gap-6">
                 @if (isTwoFactorLoading() && !twoFactorInfo()) {
                    <div class="flex justify-center p-4">
                       <i class="pi pi-spin pi-spinner text-2xl text-surface-500"></i>
                    </div>
                 } @else if (twoFactorInfo(); as tfa) {
                    
                    @if (!tfa.isTwoFactorEnabled) {
                       <div class="p-6 bg-surface-100 dark:bg-surface-800 rounded-lg flex flex-col gap-4">
                         <h3 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().enable_tfa }}</h3>
                         <p class="text-surface-600 dark:text-surface-400 m-0 leading-relaxed">
                            {{ ls.t().tfa_desc }}
                         </p>

                         <div class="flex flex-col md:flex-row gap-4 items-start">
                            @if (qrCodeDataUrl()) {
                               <div class="bg-white p-2 rounded shadow-sm flex-shrink-0">
                                  <img [src]="qrCodeDataUrl()" alt="2FA QR Code" class="w-40 h-40 block" />
                               </div>
                            }
                            <div class="bg-surface-0 dark:bg-surface-900 p-4 rounded border-l-4 border-primary flex-1 w-full">
                               <span class="block text-sm font-semibold text-surface-500 mb-1">{{ ls.t().shared_key }}</span>
                               <span class="font-mono text-lg tracking-wider text-surface-900 dark:text-surface-0 select-all">{{ tfa.sharedKey }}</span>
                            </div>
                         </div>

                         <form [formGroup]="twoFactorForm" (ngSubmit)="onEnable2FA()" class="flex flex-col gap-3 mt-2">
                           <label for="code" class="font-semibold text-surface-900 dark:text-surface-0">{{ ls.t().verify_code }}</label>
                           <input 
                              pInputText 
                              id="code" 
                              formControlName="code" 
                              [placeholder]="ls.t().digit_code" 
                              class="w-full text-lg tracking-[0.2em] font-mono text-center max-w-sm" 
                              maxlength="6"
                           />
                           <p-button 
                              type="submit" 
                              [label]="ls.t().verify_enable" 
                              [disabled]="twoFactorForm.invalid || isTwoFactorLoading()"
                              [loading]="isTwoFactorLoading()">
                           </p-button>
                         </form>
                       </div>
                    } @else {
                       <div class="p-6 bg-surface-100 dark:bg-surface-800 rounded-lg flex flex-col gap-4 border-l-4 border-success">
                         <div class="flex items-center gap-3">
                            <i class="pi pi-shield text-2xl text-green-500"></i>
                            <h3 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().tfa_enabled_title }}</h3>
                         </div>
                         <p class="text-surface-600 dark:text-surface-400 m-0" [innerHTML]="ls.translate('tfa_enabled_desc', { count: tfa.recoveryCodesLeft })"></p>
                         
                         @if (tfa.recoveryCodes && tfa.recoveryCodes.length > 0) {
                            <div class="bg-surface-0 dark:bg-surface-900 p-4 rounded border border-surface-200 dark:border-surface-700 mt-2">
                               <h4 class="font-semibold text-danger m-0 mb-3">{{ ls.t().save_recovery_codes }}</h4>
                               <p class="text-sm text-surface-500 mb-3">{{ ls.t().recovery_codes_desc }}</p>
                               <div class="grid grid-cols-2 gap-2 font-mono text-sm">
                                  @for (code of tfa.recoveryCodes; track code) {
                                     <div class="bg-surface-100 dark:bg-surface-800 p-2 rounded text-center">{{ code }}</div>
                                  }
                               </div>
                            </div>
                         }
                         
                         <div class="flex gap-3 mt-4">
                            <p-button 
                               severity="danger" 
                               [label]="ls.t().disable_tfa" 
                               (onClick)="onDisable2FA()"
                               [loading]="isTwoFactorLoading()">
                            </p-button>
                            <p-button 
                               severity="secondary" 
                               [label]="ls.t().reset_recovery" 
                               (onClick)="onResetRecoveryCodes()"
                               [loading]="isTwoFactorLoading()">
                            </p-button>
                         </div>
                       </div>
                    }
                 }
                </div>
             </p-tabpanel>

            <!-- Personal Data Tab -->
            <p-tabpanel value="personaldata">
               <div class="mt-4 flex flex-col gap-6">
                 <div class="p-6 bg-surface-100 dark:bg-surface-800 rounded-lg flex flex-col gap-4">
                   <h3 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().download_data_title }}</h3>
                   <p class="text-surface-600 dark:text-surface-400 m-0 leading-relaxed">
                      {{ ls.t().download_data_desc }}
                   </p>
                   <div class="mt-2 text-left">
                     <p-button 
                       [label]="ls.t().download_btn" 
                       (onClick)="onDownloadPersonalData()"
                       [loading]="isDownloadingPersonalData()"
                       icon="pi pi-download">
                     </p-button>
                   </div>
                 </div>

                 <div class="p-6 bg-red-50 dark:bg-red-950/20 rounded-lg flex flex-col gap-4 border border-red-200 dark:border-red-800">
                   <h3 class="text-xl font-bold m-0 text-red-700 dark:text-red-400">{{ ls.t().delete_account_title }}</h3>
                   <p class="text-red-600 dark:text-red-300 m-0 leading-relaxed">
                      {{ ls.t().delete_account_desc }}
                   </p>
                   <div class="mt-2 text-left">
                     <p-button 
                       [label]="ls.t().delete_account_btn" 
                       severity="danger"
                       (onClick)="onDeleteAccount()"
                       [loading]="isDeletingAccount()"
                       icon="pi pi-trash">
                     </p-button>
                   </div>
                 </div>
               </div>
            </p-tabpanel>
        </p-tabpanels>
      </p-tabs>
      <p-confirmDialog />
      </p-card>
    </div>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class SettingsComponent {
  authService = inject(AuthService);
  themeService = inject(ThemeService);
  ls = inject(LanguageService);
  private fb = inject(FormBuilder);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);

  form: FormGroup = this.fb.group({
    name: [''],
    surname: [''],
    phoneNumber: ['']
  });

  securityForm: FormGroup = this.fb.group({
    newEmail: ['', [Validators.email]],
    oldPassword: [''],
    newPassword: ['']
  });

  twoFactorForm: FormGroup = this.fb.group({
    code: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]]
  });

  isLoading = signal(false);
  isSecurityLoading = signal(false);
  isTwoFactorLoading = signal(false);
  isDownloadingPersonalData = signal(false);
  isDeletingAccount = signal(false);
  identityInfo = signal<InfoResponse | null>(null);
  twoFactorInfo = signal<TwoFactorResponse | null>(null);
  qrCodeDataUrl = signal<string | null>(null);

  themeOptions = [
    { label: 'Auto', value: 'auto', icon: 'pi pi-desktop' },
    { label: 'Light', value: 'light', icon: 'pi pi-sun' },
    { label: 'Dark', value: 'dark', icon: 'pi pi-moon' }
  ];

  constructor() {
    effect(() => {
      const user = this.authService.currentUser();
      if (user) {
        this.form.patchValue({
          name: user.name || '',
          surname: user.surname || '',
          phoneNumber: user.phoneNumber || ''
        }, { emitEvent: false });
      }
    });

    this.fetchIdentityInfo();
    this.fetchTwoFactorInfo();
  }

  fetchIdentityInfo() {
    this.authService.getIdentityInfo().subscribe({
      next: (info) => {
        this.identityInfo.set(info);
        this.tryGenerateQrCode();
      },
      error: (err) => console.error('Failed to fetch identity info:', err)
    });
  }

  fetchTwoFactorInfo() {
    this.isTwoFactorLoading.set(true);
    this.authService.manage2fa({}).subscribe({
      next: (info) => {
        this.twoFactorInfo.set(info);
        this.isTwoFactorLoading.set(false);
        this.tryGenerateQrCode();
      },
      error: (err) => {
        console.error('Failed to fetch 2FA info:', err);
        this.isTwoFactorLoading.set(false);
      }
    });
  }

  tryGenerateQrCode() {
    const tfa = this.twoFactorInfo();
    const identity = this.identityInfo();

    if (tfa && !tfa.isTwoFactorEnabled && identity?.email) {
      const uri = `otpauth://totp/SSSKLv2:${encodeURIComponent(identity.email)}?secret=${tfa.sharedKey}&issuer=SSSKLv2`;
      QRCode.toDataURL(uri, { width: 200, margin: 1, color: { dark: '#000000', light: '#ffffff' } })
        .then(url => {
          this.qrCodeDataUrl.set(url);
        })
        .catch(err => console.error('Failed to generate QR code', err));
    } else {
      this.qrCodeDataUrl.set(null);
    }
  }

  getRoleSeverity(role: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    const r = role.toLowerCase();
    if (r === 'admin') return 'danger';
    if (r === 'kiosk') return 'warn';
    if (r === 'user') return 'info';
    return 'secondary';
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.isLoading.set(true);
    const formData = this.form.value;

    this.authService.updateMe({
      name: formData.name || null,
      surname: formData.surname || null,
      phoneNumber: formData.phoneNumber || null
    }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.form.markAsPristine();
        this.messageService.add({
          severity: 'success',
          summary: this.ls.t().success,
          detail: this.ls.t().settings_updated
        });
      },
      error: (err) => {
        this.isLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.ls.t().error,
          detail: this.ls.t().settings_update_failed
        });
        console.error('Failed to update settings:', err);
      }
    });
  }

  isSecurityFormValid(): boolean {
    const { newEmail, oldPassword, newPassword } = this.securityForm.value;
    // Form is not "valid" to submit if all fields are empty
    if (!newEmail && !oldPassword && !newPassword) return false;

    // If changing password, it needs both old and new
    if ((oldPassword && !newPassword) || (!oldPassword && newPassword)) return false;

    return true;
  }

  onSecuritySubmit() {
    if (this.securityForm.invalid || !this.isSecurityFormValid()) return;

    this.isSecurityLoading.set(true);
    const formData = this.securityForm.value;

    const payload: any = {};
    if (formData.newEmail) payload.newEmail = formData.newEmail;
    if (formData.oldPassword) payload.oldPassword = formData.oldPassword;
    if (formData.newPassword) payload.newPassword = formData.newPassword;

    this.authService.updateIdentityInfo(payload).subscribe({
      next: (info) => {
        this.isSecurityLoading.set(false);
        // Reset the form so fields are cleared, except we don't strictly need to redirect
        this.securityForm.reset();
        this.identityInfo.set(info);
        this.messageService.add({
          severity: 'success',
          summary: this.ls.t().success,
          detail: this.ls.t().security_updated
        });
      },
      error: (err) => {
        this.isSecurityLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.ls.t().error,
          detail: this.ls.t().security_update_failed
        });
        console.error('Failed to update security settings:', err);
      }
    });
  }

  onEnable2FA() {
    if (this.twoFactorForm.invalid) return;

    this.isTwoFactorLoading.set(true);
    this.authService.manage2fa({
      enable: true,
      twoFactorCode: this.twoFactorForm.value.code
    }).subscribe({
      next: (info) => {
        this.twoFactorInfo.set(info);
        this.isTwoFactorLoading.set(false);
        this.twoFactorForm.reset();
        this.messageService.add({
          severity: 'success',
          summary: this.ls.t().success,
          detail: this.ls.t().tfa_success
        });
      },
      error: (err) => {
        this.isTwoFactorLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.ls.t().error,
          detail: this.ls.t().tfa_failed
        });
      }
    });
  }

  onDisable2FA() {
    this.isTwoFactorLoading.set(true);
    this.authService.manage2fa({ enable: false }).subscribe({
      next: (info) => {
        this.twoFactorInfo.set(info);
        this.isTwoFactorLoading.set(false);
        this.messageService.add({
          severity: 'success',
          summary: this.ls.t().success,
          detail: this.ls.t().tfa_disabled
        });
      },
      error: (err) => {
        this.isTwoFactorLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.ls.t().error,
          detail: this.ls.t().error
        });
      }
    });
  }

  onResetRecoveryCodes() {
    this.isTwoFactorLoading.set(true);
    this.authService.manage2fa({ resetRecoveryCodes: true }).subscribe({
      next: (info) => {
        this.twoFactorInfo.set(info);
        this.isTwoFactorLoading.set(false);
        this.messageService.add({
          severity: 'success',
          summary: this.ls.t().success,
          detail: this.ls.t().recovery_reset_success
        });
      },
      error: (err) => {
        this.isTwoFactorLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.ls.t().error,
          detail: this.ls.t().error
        });
      }
    });
  }

  onDownloadPersonalData() {
    this.isDownloadingPersonalData.set(true);
    this.authService.downloadPersonalData().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'PersonalData.json';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);

        this.isDownloadingPersonalData.set(false);
        this.messageService.add({
          severity: 'success',
          summary: this.ls.t().success,
          detail: this.ls.t().success
        });
      },
      error: (err) => {
        this.isDownloadingPersonalData.set(false);
        console.error('Failed to download personal data:', err);
        this.messageService.add({
          severity: 'error',
          summary: this.ls.t().error,
          detail: this.ls.t().error
        });
      }
    });
  }

  onDeleteAccount() {
    this.confirmationService.confirm({
      message: this.ls.t().confirm_delete_account,
      header: this.ls.t().confirm_delete_account_title,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-secondary p-button-text',
      accept: () => {
        this.isDeletingAccount.set(true);
        this.authService.deleteAccount().subscribe({
          next: () => {
            this.isDeletingAccount.set(false);
            this.messageService.add({
              severity: 'success',
              summary: this.ls.t().success,
              detail: this.ls.t().account_deleted
            });
            // Give some time for the message to show before logging out
            setTimeout(() => {
              this.authService.logout();
            }, 1000);
          },
          error: (err) => {
            this.isDeletingAccount.set(false);
            this.messageService.add({
              severity: 'error',
              summary: this.ls.t().error,
              detail: this.ls.t().error
            });
            console.error('Failed to delete account:', err);
          }
        });
      }
    });
  }
}
