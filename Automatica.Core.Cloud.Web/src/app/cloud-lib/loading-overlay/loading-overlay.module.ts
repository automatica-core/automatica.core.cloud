import { NgModule, ModuleWithProviders, Type } from "@angular/core";
import { CommonModule } from "@angular/common";
import { LoadingOverlayComponent } from "./loading-overlay.component";
import { DxPopupModule, DxLoadIndicatorModule } from "devextreme-angular";
import { LoadingOverlayService } from "./loading-overlay.service";

@NgModule({
  imports: [
    CommonModule,
    DxPopupModule,
    DxLoadIndicatorModule
  ],
  declarations: [LoadingOverlayComponent],
  exports: [LoadingOverlayComponent]
})
export class LoadingOverlayModule {
  static forRoot(): ModuleWithProviders<LoadingOverlayModule> {
    return {
      ngModule: LoadingOverlayModule,
      providers: [
        LoadingOverlayService
      ],
    };
  }

  static forChild(component: Type<any>): ModuleWithProviders<LoadingOverlayModule> {
    return {
      ngModule: LoadingOverlayModule
    };
  }
}
