import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { CloudEditorComponent } from "./cloud-editor.component";
import { L10nTranslationModule } from "angular-l10n";

@NgModule({
  imports: [
    CommonModule,
    L10nTranslationModule
  ],
  declarations: [CloudEditorComponent],
  exports: [CloudEditorComponent]
})
export class CloudEditorModule { }
