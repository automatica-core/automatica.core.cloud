import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ResetPasswordComponent } from "./reset-password.component";
import { TranslationModule } from "angular-l10n";
import { BrowserModule } from "@angular/platform-browser";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { MatButtonModule, MatInputModule, MatCommonModule, MatIconModule, MatFormFieldModule } from "@angular/material";
import { RouterModule } from "@angular/router";

@NgModule({
  imports: [
    CommonModule,
    RouterModule,
    TranslationModule,
    BrowserModule,
    BrowserAnimationsModule,
    CommonModule,
    MatButtonModule,
    MatInputModule,
    MatCommonModule,
    RouterModule,
    MatIconModule,
    MatFormFieldModule,
  ],
  declarations: [ResetPasswordComponent],
  exports: [ResetPasswordComponent]
})
export class ResetPasswordModule { }
