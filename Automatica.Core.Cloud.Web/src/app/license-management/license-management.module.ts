import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { LicenseManagementComponent } from "./license-management.component";
import { LicenseDetailComponent } from "./license-detail/license-detail.component";
import { FormsModule } from "@angular/forms";
import { MatIconModule, MatButtonModule, MatFormFieldModule, MatCheckboxModule, MatInputModule } from "@angular/material";
import { TranslationModule } from "angular-l10n";
import { CloudLibModule } from "../cloud-lib/cloud-lib.module";
import { MasterDetailModule } from "../cloud-lib/master-detail/master-detail.module";
import { CloudFormModule } from "../cloud-lib/cloud-form/cloud-form.module";
import { CloudListModule } from "../cloud-lib/cloud-list/cloud-list.module";
import { DxFileUploaderModule, DxSelectBoxModule, DxCheckBoxModule, DxTextBoxModule, DxTextAreaModule, DxNumberBoxModule, DxDataGridModule } from "devextreme-angular";
import { LicenseManagementRoutingModule } from "./license-management.routing.module";
import { LicenseManagementService } from "./license-management.service";
import { BaseFormService } from "../base/base-form.service";
import { ServerConnectionsService } from "../server-connections-management/server-connections.service";
import { CloudEditorModule } from "../cloud-lib/cloud-editor/cloud-editor.module";

@NgModule({
  imports: [
    FormsModule,
    CommonModule,

    TranslationModule,
    CloudLibModule,
    MasterDetailModule,
    CloudFormModule,
    CloudListModule,
    DxFileUploaderModule,
    DxSelectBoxModule,
    DxCheckBoxModule,
    DxTextBoxModule,
    DxDataGridModule,
    CloudEditorModule,
    DxNumberBoxModule,
    DxTextAreaModule,
    LicenseManagementRoutingModule
  ],
  declarations: [LicenseManagementComponent, LicenseDetailComponent],
  providers: [
    LicenseManagementService,
    { provide: BaseFormService, useExisting: LicenseManagementService },
    ServerConnectionsService
  ]
})
export class LicenseManagementModule { }
