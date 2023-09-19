import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { DxFileUploaderModule, DxTextBoxModule, DxCheckBoxModule, DxTextAreaModule, DxButtonModule, DxSelectBoxModule } from "devextreme-angular";
import { L10nTranslationModule } from "angular-l10n";
import { FormsModule } from "@angular/forms";
import { CloudLibModule } from "../cloud-lib/cloud-lib.module";
import { MasterDetailModule } from "../cloud-lib/master-detail/master-detail.module";
import { CloudFormModule } from "../cloud-lib/cloud-form/cloud-form.module";
import { BaseFormService } from "../base/base-form.service";
import { CloudListModule } from "../cloud-lib/cloud-list/cloud-list.module";
import { CloudEditorModule } from "../cloud-lib/cloud-editor/cloud-editor.module";
import { ServerDockerVersionsManagementComponent } from "./server-docker-versions-management.component";
import { ServerDockerVersionsDetailComponent } from "./server-docker-versions-detail/server-docker-versions-detail.component";
import { ServerDockerVersionsManagementRoutingModule } from "./server-docker-version-management.routing.module";
import { ServerDockerVersionService } from "./server-docker-versions.service";

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

    ServerDockerVersionsManagementRoutingModule
  ],
  declarations: [ServerDockerVersionsManagementComponent, ServerDockerVersionsDetailComponent],
  exports: [ServerDockerVersionsManagementComponent],
  providers: [
    ServerDockerVersionService,
    { provide: BaseFormService, useExisting: ServerDockerVersionService }
  ]
})
export class ServerDockerVersionsManagementModule { }
