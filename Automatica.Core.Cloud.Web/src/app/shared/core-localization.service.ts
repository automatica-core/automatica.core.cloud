
import { L10nConfig, L10nLoader, L10nProvider, L10nTranslationLoader, L10nTranslationService } from "angular-l10n";
import { Injectable, Optional } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";


// import "devextreme-intl";

import * as deMessages from "devextreme/localization/messages/de.json";
import * as enMessages from "devextreme/localization/messages/en.json";
import { locale, loadMessages } from "devextreme/localization"
import { Observable } from "rxjs";

@Injectable() export class HttpTranslationLoader implements L10nTranslationLoader {

  private headers = new HttpHeaders({ "Content-Type": "application/json" });

  constructor(@Optional() private http: HttpClient) { }

  public get(language: string, provider: L10nProvider): Observable<{ [key: string]: any }> {

    if (provider.options.type === "file") {

      const url = `${provider.asset}-${language}.json`;
      const options = {
        headers: this.headers
      };
      return this.http.get(url, options);

    } else if (provider.options.type === "webapi") {

      const url = `${provider.asset}/${language}`;
      const options = {
        headers: this.headers
      };

      return this.http.get(url, options);
    }
  }

}
export const CoreTranslationConfig: L10nConfig = {
  format: "language-region",
  cache: false,
  keySeparator: ".",
  defaultLocale: { language: "de", currency: "EUR" },
  schema: [
    { locale: { language: "de", currency: "EUR" }, dir: "ltr", text: "German" },
    { locale: { language: "en", currency: "USD" }, dir: "ltr", text: "English" }
  ],
  providers: [
    { name: "app", asset: "./assets/locale/common", options: { type: "file" } }
  ]
};


@Injectable()
export class CoreTranslationService {
  constructor(public translation: L10nTranslationService, private http: HttpClient, private l10Loader: L10nLoader) {
    //this.translation.onError((error: any) => console.log(error));
  }

  async init() {
    await this.l10Loader.init();

    loadMessages(deMessages);
    loadMessages(enMessages);

    
    locale("de");
  }

}
