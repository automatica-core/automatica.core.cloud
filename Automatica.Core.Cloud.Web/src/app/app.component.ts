import { Component, OnInit } from "@angular/core";
import * as jQuery from "jquery";
import { L10nLoader } from "angular-l10n";
import { CoreTranslationService } from "./shared/core-localization.service";

@Component({
  selector: "app-root",
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.css"]
})
export class AppComponent implements OnInit {

  title = "automatica-cloud";

  constructor(private translateService: CoreTranslationService) {

  }

  async ngOnInit() {
    await this.translateService.init();
  }
}
