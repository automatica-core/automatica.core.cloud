import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ResetPasswordComponent } from "./reset-password.component";
import { L10nTranslationModule } from "angular-l10n";
import { BrowserModule } from "@angular/platform-browser";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatCommonModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';

import { RouterModule } from "@angular/router";

@NgModule({
  imports: [
    CommonModule,
    RouterModule,
    L10nTranslationModule,
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
