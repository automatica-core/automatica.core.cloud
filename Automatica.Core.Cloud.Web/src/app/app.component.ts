import { Component, OnInit } from "@angular/core";
import { CoreTranslationService } from "./shared/core-localization.service";
import { FaConfig, FaIconLibrary } from "@fortawesome/angular-fontawesome";
import { faQuestion, fas } from "@fortawesome/free-solid-svg-icons";
import { far } from "@fortawesome/free-regular-svg-icons";

@Component({
  selector: "app-root",
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.css"]
})
export class AppComponent implements OnInit {

  title = "automatica-cloud";

  constructor(private translateService: CoreTranslationService,
    library: FaIconLibrary,
    iconConfig: FaConfig) {
      library.addIconPacks(fas);
      library.addIconPacks(far);

      
    iconConfig.fallbackIcon = faQuestion;
  }

  async ngOnInit() {
    console.log("yepppp");
    try {
      await this.translateService.init();
    }
    catch (e) {
      console.log(e);
    }
  }
}
