import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { CloudEditorComponent } from "./cloud-editor.component";
import { TranslationModule } from "angular-l10n";

@NgModule({
  imports: [
    CommonModule,
    TranslationModule
  ],
  declarations: [CloudEditorComponent],
  exports: [CloudEditorComponent]
})
export class CloudEditorModule { }
