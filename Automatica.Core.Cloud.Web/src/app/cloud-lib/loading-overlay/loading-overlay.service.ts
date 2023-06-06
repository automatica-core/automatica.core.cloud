import { Injectable } from "@angular/core";
import { L10nTranslationService } from "angular-l10n";

@Injectable()
export class LoadingOverlayService {

  title: string = "";
  popupVisible: boolean = false;

  constructor(private translate: L10nTranslationService) { }

  showLoadingPanel(title: string) {
    this.title = this.translate.translate(title);
    this.popupVisible = true;
  }

  hideLoadingPanel() {
    this.popupVisible = false;
    this.title = "";
  }
}
