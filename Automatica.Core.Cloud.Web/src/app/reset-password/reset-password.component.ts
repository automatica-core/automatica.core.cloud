import { Component, OnInit } from "@angular/core";
import { Language } from "angular-l10n";

@Component({
  selector: "core-reset-password",
  templateUrl: "./reset-password.component.html",
  styleUrls: ["./reset-password.component.scss"]
})
export class ResetPasswordComponent implements OnInit {

  @Language() lang: string;
  
  constructor() { }

  ngOnInit() {
  }

}
