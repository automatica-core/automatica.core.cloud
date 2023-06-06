import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ServerConnectionsManagementComponent } from "./server-connections-management.component";
import { ServerConnectionsDetailComponent } from "./server-connections-detail/server-connections-detail.component";
import { FormsModule } from "@angular/forms";
import { L10nTranslationModule } from "angular-l10n";
import { CloudLibModule } from "../cloud-lib/cloud-lib.module";
import { MasterDetailModule } from "../cloud-lib/master-detail/master-detail.module";
import { CloudFormModule } from "../cloud-lib/cloud-form/cloud-form.module";
import { CloudListModule } from "../cloud-lib/cloud-list/cloud-list.module";
import { ServerConnectionsService } from "./server-connections.service";
import { BaseFormService } from "../base/base-form.service";
import { ServerConnectionsManagementRoutingModule } from "./server-connections-management.routing.module";
import { DxTextBoxModule } from "devextreme-angular";
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
    ServerConnectionsManagementRoutingModule,

    DxTextBoxModule,
    CloudEditorModule
  ],
  declarations: [ServerConnectionsManagementComponent, ServerConnectionsDetailComponent],
  exports: [ServerConnectionsManagementComponent],
  providers: [
    ServerConnectionsService,
    { provide: BaseFormService, useExisting: ServerConnectionsService }
  ]
})
export class ServerConnectionsManagementModule { }
