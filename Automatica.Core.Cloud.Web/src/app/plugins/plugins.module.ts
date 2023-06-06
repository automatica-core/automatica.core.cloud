import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { PluginsComponent } from "./plugins.component";
import { PluginDetailComponent } from "./plugin-detail/plugin-detail.component";
import { FormsModule } from "@angular/forms";
import { L10nTranslationModule } from "angular-l10n";
import { CloudLibModule } from "../cloud-lib/cloud-lib.module";
import { MasterDetailModule } from "../cloud-lib/master-detail/master-detail.module";
import { CloudFormModule } from "../cloud-lib/cloud-form/cloud-form.module";
import { CloudListModule } from "../cloud-lib/cloud-list/cloud-list.module";
import { DxFileUploaderModule, DxSelectBoxModule, DxFormModule, DxTextBoxModule, DxCheckBoxModule, DxButtonModule, DxPopupModule } from "devextreme-angular";
import { PluginsRoutingModule } from "./plugins.routing.module";
import { PluginsFormService } from "./plugins-form.service";
import { BaseFormService } from "../base/base-form.service";
import { CloudEditorModule } from "../cloud-lib/cloud-editor/cloud-editor.module";

@NgModule({
  imports: [
    FormsModule,
    CommonModule,
    L10nTranslationModule,
    CloudLibModule,
    MasterDetailModule,
    CloudFormModule,
    CloudListModule,
    DxFileUploaderModule,
    DxSelectBoxModule,
    PluginsRoutingModule,
    DxFormModule,
    CloudEditorModule,
    DxTextBoxModule,
    DxCheckBoxModule,
    DxButtonModule,
    DxPopupModule
  ],
  declarations: [PluginsComponent, PluginDetailComponent],
  providers: [
    PluginsFormService,
    { provide: BaseFormService, useExisting: PluginsFormService }]
})
export class PluginsModule { }
