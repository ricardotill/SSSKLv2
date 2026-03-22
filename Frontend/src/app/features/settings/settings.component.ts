import { Component, ChangeDetectionStrategy, inject, OnInit, effect, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, FormGroup } from '@angular/forms';
import { AuthService, InfoResponse, TwoFactorResponse } from '../../core/auth/auth.service';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { TabsModule } from 'primeng/tabs';
import * as QRCode from 'qrcode';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, ToastModule, TagModule, TabsModule],
  providers: [MessageService],
  template: `
    <div class="bg-surface-0 dark:bg-surface-900 p-8 rounded-xl shadow-md text-surface-900 dark:text-surface-0 max-w-3xl mx-auto mt-8">
      <p-toast></p-toast>
      <h2 class="text-2xl font-bold mb-6">User Settings</h2>

      <p-tabs value="profile">
        <p-tablist>
            <p-tab value="profile">Profile</p-tab>
            <p-tab value="security">Account Security</p-tab>
            <p-tab value="twofactor">2FA Settings</p-tab>
            <p-tab value="personaldata">Personal Data</p-tab>
        </p-tablist>
        
        <p-tabpanels>
            <!-- Profile Tab -->
            <p-tabpanel value="profile">
              <div class="mt-4">
                @if (authService.currentUser(); as user) {
                  <div class="mb-8">
                    <h3 class="text-xl font-semibold mb-3">Roles</h3>
                    <div class="flex gap-2 flex-wrap">
                      @for (role of user.roles; track role) {
                        <p-tag [value]="role" [severity]="getRoleSeverity(role)"></p-tag>
                      }
                      @if (!user.roles || user.roles.length === 0) {
                        <span class="text-surface-500 text-sm">No roles assigned</span>
                      }
                    </div>
                  </div>
                }

                <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-5">
                  <div class="flex flex-col gap-2">
                    <label for="name" class="font-semibold cursor-pointer">First Name</label>
                    <input pInputText id="name" formControlName="name" class="w-full text-lg p-3" />
                  </div>

                  <div class="flex flex-col gap-2">
                    <label for="surname" class="font-semibold cursor-pointer">Last Name</label>
                    <input pInputText id="surname" formControlName="surname" class="w-full text-lg p-3" />
                  </div>

                  <div class="flex flex-col gap-2">
                    <label for="phoneNumber" class="font-semibold cursor-pointer">Phone Number</label>
                    <input pInputText id="phoneNumber" formControlName="phoneNumber" class="w-full text-lg p-3" />
                  </div>

                  <div class="flex justify-end mt-4">
                    <p-button 
                      type="submit" 
                      label="Save Changes" 
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
                       <h3 class="font-semibold text-lg hover:text-primary transition-colors cursor-pointer">Current Email Status</h3>
                       <div class="flex items-center gap-3">
                         <span class="text-surface-700 dark:text-surface-300 font-medium">{{ info.email }}</span>
                         @if (info.isEmailConfirmed) {
                           <p-tag severity="success" value="Confirmed"></p-tag>
                         } @else {
                           <p-tag severity="warn" value="Unconfirmed"></p-tag>
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
                     <label for="newEmail" class="font-semibold cursor-pointer">New Email Address</label>
                     <input pInputText id="newEmail" type="email" formControlName="newEmail" class="w-full text-lg p-3" placeholder="Leave blank to keep current email" />
                   </div>
                   
                   <div class="flex flex-col gap-2 mt-4">
                     <label for="oldPassword" class="font-semibold cursor-pointer">Current Password</label>
                     <input pInputText id="oldPassword" type="password" formControlName="oldPassword" class="w-full text-lg p-3" placeholder="Required if changing your password" />
                   </div>
                   
                   <div class="flex flex-col gap-2">
                     <label for="newPassword" class="font-semibold cursor-pointer">New Password</label>
                     <input pInputText id="newPassword" type="password" formControlName="newPassword" class="w-full text-lg p-3" placeholder="Leave blank to keep current password" />
                   </div>
                   
                   <div class="flex justify-end mt-4">
                     <p-button 
                       type="submit" 
                       label="Update Security" 
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
                         <h3 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">Enable Two-Factor Authentication</h3>
                         <p class="text-surface-600 dark:text-surface-400 m-0 leading-relaxed">
                            Protect your account by enabling 2FA. Open your authenticator app (like Google Authenticator or Authy) and scan the QR code below, or manually enter the shared key.
                         </p>

                         <div class="flex flex-col md:flex-row gap-4 items-start">
                            @if (qrCodeDataUrl()) {
                               <div class="bg-white p-2 rounded shadow-sm flex-shrink-0">
                                  <img [src]="qrCodeDataUrl()" alt="2FA QR Code" class="w-40 h-40 block" />
                               </div>
                            }
                            <div class="bg-surface-0 dark:bg-surface-900 p-4 rounded border-l-4 border-primary flex-1 w-full">
                               <span class="block text-sm font-semibold text-surface-500 mb-1">Shared Key</span>
                               <span class="font-mono text-lg tracking-wider text-surface-900 dark:text-surface-0 select-all">{{ tfa.sharedKey }}</span>
                            </div>
                         </div>

                         <form [formGroup]="twoFactorForm" (ngSubmit)="onEnable2FA()" class="flex flex-col gap-3 mt-2">
                           <label for="code" class="font-semibold text-surface-900 dark:text-surface-0">Verify Code</label>
                           <input 
                              pInputText 
                              id="code" 
                              formControlName="code" 
                              placeholder="6-digit code" 
                              class="w-full text-lg tracking-[0.2em] font-mono text-center max-w-sm" 
                              maxlength="6"
                           />
                           <p-button 
                              type="submit" 
                              label="Verify & Enable" 
                              [disabled]="twoFactorForm.invalid || isTwoFactorLoading()"
                              [loading]="isTwoFactorLoading()">
                           </p-button>
                         </form>
                       </div>
                    } @else {
                       <div class="p-6 bg-surface-100 dark:bg-surface-800 rounded-lg flex flex-col gap-4 border-l-4 border-success">
                         <div class="flex items-center gap-3">
                            <i class="pi pi-shield text-2xl text-green-500"></i>
                            <h3 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">Two-Factor Authentication is Enabled</h3>
                         </div>
                         <p class="text-surface-600 dark:text-surface-400 m-0">Your account is secured. You have <strong>{{ tfa.recoveryCodesLeft }}</strong> recovery codes remaining.</p>
                         
                         @if (tfa.recoveryCodes && tfa.recoveryCodes.length > 0) {
                            <div class="bg-surface-0 dark:bg-surface-900 p-4 rounded border border-surface-200 dark:border-surface-700 mt-2">
                               <h4 class="font-semibold text-danger m-0 mb-3">⚠️ Save these recovery codes</h4>
                               <p class="text-sm text-surface-500 mb-3">These codes will not be shown again. Store them in a secure place like a password manager.</p>
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
                               label="Disable 2FA" 
                               (onClick)="onDisable2FA()"
                               [loading]="isTwoFactorLoading()">
                            </p-button>
                            <p-button 
                               severity="secondary" 
                               label="Reset Recovery Codes" 
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
                   <h3 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">Download Personal Data</h3>
                   <p class="text-surface-600 dark:text-surface-400 m-0 leading-relaxed">
                      You can request a copy of your personal data associated with your account. This downloaded file will contain all information we securely store about you.
                   </p>
                   <div class="mt-2 text-left">
                     <p-button 
                       label="Download My Data" 
                       (onClick)="onDownloadPersonalData()"
                       [loading]="isDownloadingPersonalData()"
                       icon="pi pi-download">
                     </p-button>
                   </div>
                 </div>
               </div>
            </p-tabpanel>
        </p-tabpanels>
      </p-tabs>
    </div>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class SettingsComponent { 
  authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private messageService = inject(MessageService);

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
  identityInfo = signal<InfoResponse | null>(null);
  twoFactorInfo = signal<TwoFactorResponse | null>(null);
  qrCodeDataUrl = signal<string | null>(null);

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
          summary: 'Success',
          detail: 'Your settings have been updated.'
        });
      },
      error: (err) => {
        this.isLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update settings. Please try again.'
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
          summary: 'Success',
          detail: 'Your security settings have been updated.'
        });
      },
      error: (err) => {
        this.isSecurityLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update security settings. Please check your inputs.'
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
          summary: 'Success',
          detail: 'Two-Factor Authentication enabled successfully.'
        });
      },
      error: (err) => {
        this.isTwoFactorLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to enable 2FA. Please verify your code and try again.'
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
          summary: 'Success',
          detail: 'Two-Factor Authentication disabled.'
        });
      },
      error: (err) => {
        this.isTwoFactorLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to disable 2FA.'
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
          summary: 'Success',
          detail: 'Recovery codes reset successfully.'
        });
      },
      error: (err) => {
        this.isTwoFactorLoading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to reset recovery codes.'
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
          summary: 'Success',
          detail: 'Personal data downloaded successfully.'
        });
      },
      error: (err) => {
        this.isDownloadingPersonalData.set(false);
        console.error('Failed to download personal data:', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to download personal data. Please try again later.'
        });
      }
    });
  }
}
