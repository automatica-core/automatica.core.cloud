import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { CloudListComponent } from "./cloud-list.component";
import { DxDataGridModule, DxButtonModule } from "devextreme-angular";
import { L10nTranslationModule } from "angular-l10n";
import { LoadingOverlayModule } from "../loading-overlay/loading-overlay.module";

@NgModule({
  imports: [
    CommonModule,
    DxDataGridModule,
    L10nTranslationModule,
    DxButtonModule,
    LoadingOverlayModule
  ],
  declarations: [CloudListComponent],
  exports: [CloudListComponent]
})
export class CloudListModule { }
