import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { AngularSplitModule } from "angular-split";
import { MasterDetailComponent } from "./master-detail.component";

@NgModule({
  imports: [
    CommonModule,
    AngularSplitModule
  ],
  declarations: [MasterDetailComponent],
  exports: [MasterDetailComponent]
})
export class MasterDetailModule { }
