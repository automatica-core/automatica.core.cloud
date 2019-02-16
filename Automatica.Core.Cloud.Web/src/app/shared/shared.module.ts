import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { SlimScrollDirective } from "./slim-scroll.directive";
import { AuthGuard } from "./auth.service";

@NgModule({
  imports: [
    CommonModule
  ],
  declarations: [SlimScrollDirective],
  providers: [
    AuthGuard
  ]
})
export class SharedModule { }
