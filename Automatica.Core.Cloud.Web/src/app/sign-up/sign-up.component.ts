import { Component, OnInit } from "@angular/core";
import { Language } from "angular-l10n";

@Component({
  selector: "core-sign-up",
  templateUrl: "./sign-up.component.html",
  styleUrls: ["./sign-up.component.scss"]
})
export class SignUpComponent implements OnInit {

  @Language() lang: string;

  constructor() { }

  ngOnInit() {
  }

}
