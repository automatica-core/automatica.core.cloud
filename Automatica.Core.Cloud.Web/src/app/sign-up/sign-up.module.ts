import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { SignUpComponent } from "./sign-up.component";
import { TranslationModule } from "angular-l10n";
import { BrowserModule } from "@angular/platform-browser";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { MatButtonModule, MatInputModule, MatCommonModule, MatIconModule, MatFormFieldModule } from "@angular/material";
import { RouterModule } from "@angular/router";

@NgModule({
  imports: [
    CommonModule,
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
  declarations: [SignUpComponent],
  exports: [SignUpComponent]
})
export class SignUpModule { }
