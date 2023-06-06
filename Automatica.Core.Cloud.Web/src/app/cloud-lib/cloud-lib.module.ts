import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MasterDetailModule } from "./master-detail/master-detail.module";
import { L10nTranslationModule } from "angular-l10n";
import { FontAwesomeModule } from "@fortawesome/angular-fontawesome";

@NgModule({
  imports: [
    CommonModule,
    MasterDetailModule,
    L10nTranslationModule,
    FontAwesomeModule
  ],
  declarations: []
})
export class CloudLibModule { }
