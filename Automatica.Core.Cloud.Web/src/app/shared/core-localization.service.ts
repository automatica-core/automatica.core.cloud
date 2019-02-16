
import { StorageStrategy, ProviderType, L10nConfig, L10nLoader, LocaleService, TranslationService } from "angular-l10n";
import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";


import "devextreme-intl";

import * as deMessages from "devextreme/localization/messages/de.json";
import * as enMessages from "devextreme/localization/messages/en.json";
import { locale, loadMessages } from "devextreme/localization"

export const CoreTranslationConfig: L10nConfig = {
  locale: {
    languages: [
      { code: "en", dir: "ltr" },
      { code: "de", dir: "ltr" }
    ],
    language: "de",
    defaultLocale: {
      countryCode: "US",
      languageCode: "en"
    },
    currency: "€",
    storage: StorageStrategy.Disabled
  },
  translation: {
    providers: [
      { type: ProviderType.Static, prefix: "./assets/locale/common-" }
    ],
    caching: true,
    composedKeySeparator: "."
  }
};


@Injectable()
export class CoreTranslationService {
  constructor(public localeService: LocaleService, public translation: TranslationService, private http: HttpClient, private l10Loader: L10nLoader) {
    this.translation.translationError.subscribe((error: any) => console.log(error));
  }

  async init() {
    await this.l10Loader.load();

    loadMessages(deMessages);
    loadMessages(enMessages);

  }

}
