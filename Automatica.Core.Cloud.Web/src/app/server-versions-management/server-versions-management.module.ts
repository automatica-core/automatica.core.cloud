import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ServerVersionsManagementComponent } from "./server-versions-management.component";
import { AngularSplitModule } from "angular-split";
import { DxDataGridModule, DxFileUploaderModule, DxTextBoxModule, DxCheckBoxModule, DxTextAreaModule, DxButtonModule, DxSelectBoxModule } from "devextreme-angular";
import { L10nTranslationModule } from "angular-l10n";
import { FormsModule } from "@angular/forms";
import { CloudLibModule } from "../cloud-lib/cloud-lib.module";
import { MasterDetailModule } from "../cloud-lib/master-detail/master-detail.module";
import { CloudFormModule } from "../cloud-lib/cloud-form/cloud-form.module";
import { ServerVersionsDetailComponent } from "./server-versions-detail/server-versions-detail.component";
import { ServerVersionService } from "./server-versions.service";
import { BaseFormService } from "../base/base-form.service";
import { CloudListModule } from "../cloud-lib/cloud-list/cloud-list.module";
import { ServerVersionsManagementRoutingModule } from "./server-version-management.routing.module";
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
    CloudEditorModule,
    DxTextBoxModule,
    DxCheckBoxModule,
    DxTextAreaModule,
    DxFileUploaderModule,
    DxButtonModule,
    DxSelectBoxModule,

    ServerVersionsManagementRoutingModule
  ],
  declarations: [ServerVersionsManagementComponent, ServerVersionsDetailComponent],
  exports: [ServerVersionsManagementComponent],
  providers: [
    ServerVersionService,
    { provide: BaseFormService, useExisting: ServerVersionService }
  ]
})
export class ServerVersionsManagementModule { }
