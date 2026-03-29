import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { finalize } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ProductService } from '../../../core/services/product.service';
import { ProductDto, ProductCreateDto, ProductUpdateDto, PaginatedProducts } from '../../../core/models/product.model';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    ConfirmDialogModule,
    ConfirmDialogModule,
    CardModule,
    ToggleSwitchModule
  ],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().products }}</h1>
      <div class="flex gap-2">
        <p-button icon="pi pi-plus" severity="info" [rounded]="true" (onClick)="openCreateDialog()" [ariaLabel]="ls.t().add_product"></p-button>
        <p-button icon="pi pi-refresh" [rounded]="true" (onClick)="loadProducts()" [ariaLabel]="ls.t().refresh"></p-button>
      </div>
    </div>
    <p-confirmDialog></p-confirmDialog>
    <p-card>
      <p-table stripedRows [value]="products()" [loading]="loading()" [paginator]="true" [rows]="10" [totalRecords]="totalRecords()" responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th>{{ ls.t().product_name }}</th>
            <th>{{ ls.t().product_description }}</th>
            <th>{{ ls.t().price }}</th>
            <th>{{ ls.t().stock }}</th>
            <th class="text-center">{{ ls.t().leaderboard }}</th>
            <th class="w-32">{{ ls.t().actions }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-product>
          <tr>
            <td>{{ product.name }}</td>
            <td>{{ product.description }}</td>
            <td>{{ product.price | currency:'EUR' }}</td>
            <td>{{ product.stock }}</td>
            <td class="text-center">
              <i class="pi" [class.pi-check-circle]="product.enableLeaderboard" [class.text-primary-500]="product.enableLeaderboard" [class.pi-times-circle]="!product.enableLeaderboard" [class.text-red-500]="!product.enableLeaderboard"></i>
            </td>
            <td>
              <div class="flex gap-2">
                <p-button icon="pi pi-pencil" [rounded]="true" [text]="true" severity="info" (onClick)="openEditDialog(product)" [ariaLabel]="ls.t().edit"></p-button>
                <p-button icon="pi pi-trash" [rounded]="true" [text]="true" severity="danger" (onClick)="confirmDelete(product)" [loading]="deletingProductId() === product.id" [ariaLabel]="ls.t().delete"></p-button>
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="5" class="text-center p-4 text-surface-500">{{ ls.t().no_products }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>

    <p-dialog [header]="isEdit() ? ls.t().edit_product : ls.t().add_product" [(visible)]="dialogVisible" [modal]="true" [style]="{width: '500px'}" [breakpoints]="{'768px': '90vw'}">
      <form [formGroup]="productForm" (ngSubmit)="saveProduct()" class="flex flex-col gap-4 mt-2">
        <div class="flex flex-col gap-2">
          <label for="name">{{ ls.t().product_name }}</label>
          <input pInputText id="name" formControlName="name" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="description">{{ ls.t().product_description }}</label>
          <input pInputText id="description" formControlName="description" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="price">{{ ls.t().price }}</label>
          <p-inputNumber id="price" formControlName="price" mode="currency" currency="EUR" locale="nl-NL" class="w-full" [min]="0"></p-inputNumber>
        </div>

        <div class="flex flex-col gap-2">
          <label for="stock">{{ ls.t().stock }}</label>
          <p-inputNumber id="stock" formControlName="stock" class="w-full" [min]="0" [showButtons]="true"></p-inputNumber>
        </div>

        <div class="flex items-center gap-3">
          <p-toggleSwitch id="enableLeaderboard" formControlName="enableLeaderboard"></p-toggleSwitch>
          <label for="enableLeaderboard">{{ ls.t().enable_leaderboard }}</label>
        </div>
      </form>
      
      <ng-template pTemplate="footer">
        @if (isEdit()) {
          <p-button [label]="ls.t().view_livedisplay" icon="pi pi-external-link" [text]="true" severity="info" (onClick)="openLiveDisplay()"></p-button>
        }
        <p-button [label]="ls.t().cancel" icon="pi pi-times" [text]="true" severity="secondary" (onClick)="dialogVisible.set(false)"></p-button>
        <p-button [label]="ls.t().save" icon="pi pi-check" (onClick)="saveProduct()" [loading]="saving()" [disabled]="productForm.invalid"></p-button>
      </ng-template>
    </p-dialog>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService]
})
export default class ProductsComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  ls = inject(LanguageService);

  products = signal<ProductDto[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  dialogVisible = signal<boolean>(false);
  isEdit = signal<boolean>(false);
  editingProductId = signal<string | null>(null);
  saving = signal<boolean>(false);
  deletingProductId = signal<string | null>(null);

  productForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    price: [0, [Validators.required, Validators.min(0)]],
    stock: [0, [Validators.required, Validators.min(0)]],
    enableLeaderboard: [true]
  });

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.productService.getProducts().subscribe({
      next: (data) => {
        const paginatedData = data as PaginatedProducts;
        this.products.set(paginatedData.items);
        this.totalRecords.set(paginatedData.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        this.loading.set(false);
      }
    });
  }

  openCreateDialog(): void {
    this.isEdit.set(false);
    this.editingProductId.set(null);
    this.productForm.reset({
      name: '',
      description: '',
      price: 0,
      stock: 0,
      enableLeaderboard: true
    });
    this.dialogVisible.set(true);
  }

  openEditDialog(product: ProductDto): void {
    this.isEdit.set(true);
    this.editingProductId.set(product.id);
    this.productForm.patchValue({
      name: product.name,
      description: product.description ?? '',
      price: product.price,
      stock: product.stock,
      enableLeaderboard: product.enableLeaderboard
    });
    this.dialogVisible.set(true);
  }

  openLiveDisplay(): void {
    const id = this.editingProductId();
    if (id) {
      window.open(`/leaderboard/livedisplay/${id}`, '_blank');
    }
  }

  saveProduct(): void {
    if (this.productForm.invalid) return;

    this.saving.set(true);
    const formValue = this.productForm.getRawValue();

    if (this.isEdit() && this.editingProductId()) {
      const updateDto: ProductUpdateDto = {
        id: this.editingProductId()!,
        ...formValue,
        description: formValue.description || null
      };
      this.productService.updateProduct(updateDto.id, updateDto).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().product_updated });
          this.dialogVisible.set(false);
          this.saving.set(false);
          this.loadProducts();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().update_failed });
          this.saving.set(false);
        }
      });
    } else {
      const createDto: ProductCreateDto = {
        ...formValue,
        description: formValue.description || null
      };
      this.productService.createProduct(createDto).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().product_added });
          this.dialogVisible.set(false);
          this.saving.set(false);
          this.loadProducts();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
          this.saving.set(false);
        }
      });
    }
  }

  confirmDelete(product: ProductDto): void {
    this.confirmationService.confirm({
      message: this.ls.translate('confirm_delete_product', { name: product.name }),
      header: this.ls.t().confirm_delete_title,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteProduct(product.id);
      }
    });
  }

  deleteProduct(id: string): void {
    this.deletingProductId.set(id);
    this.productService.deleteProduct(id)
      .pipe(finalize(() => this.deletingProductId.set(null)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().product_deleted });
          this.loadProducts();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().delete_failed });
        }
      });
  }
}
