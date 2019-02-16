import { BrowserModule } from "@angular/platform-browser";
import { NgModule } from "@angular/core";

import { AppComponent } from "./app.component";
import { AppRoutingModule } from "./app.routing.module";
import { SharedModule } from "./shared/shared.module";
import { LoginModule } from "./login/login.module";
import { BASE_PATH, ApiModule, Configuration, UserService } from "./webapi";
import { environment } from "src/environments/environment";
import { LocalizationModule, LocaleValidationModule, L10nLoader } from "angular-l10n";
import { CoreTranslationConfig, CoreTranslationService } from "./shared/core-localization.service";
import { SignUpModule } from "./sign-up/sign-up.module";
import { ResetPasswordModule } from "./reset-password/reset-password.module";
import { LoadingOverlayModule } from "./cloud-lib/loading-overlay/loading-overlay.module";
import { UserDataService } from "./shared/user-data.service";
import { HttpClientModule } from "@angular/common/http";

@NgModule({
  declarations: [
    AppComponent,
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    SharedModule,
    LoginModule,
    ApiModule.forRoot(() => {
      const conf = new Configuration({
        basePath: "/webapi/v1/"
      });
      conf.apiKeys = {};
      conf.apiKeys["Authorization"] = "Bearer " + localStorage.getItem("token");

      return conf;
    }),
    LocalizationModule.forRoot(CoreTranslationConfig),
    LocaleValidationModule.forRoot(),
    SignUpModule,
    ResetPasswordModule,
    LoadingOverlayModule,
    LoadingOverlayModule.forRoot()
    ],
  providers: [
    { provide: BASE_PATH, useValue: environment.API_BASE_PATH },
    CoreTranslationService,
    UserDataService
  ],
  bootstrap: [AppComponent]
})
export class AppModule {

}
