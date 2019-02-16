import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { CloudFormComponent } from "./cloud-form.component";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material";
import { TranslationModule } from "angular-l10n";
import { DxFormModule, DxButtonModule, DxBoxModule } from "devextreme-angular";

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    TranslationModule,
    DxBoxModule,
    DxButtonModule
  ],
  declarations: [CloudFormComponent],
  exports: [CloudFormComponent]
})
export class CloudFormModule { }
