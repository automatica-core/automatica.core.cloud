import { NgModule, ModuleWithProviders, SkipSelf, Optional } from '@angular/core';
import { Configuration } from './configuration';
import { HttpClient } from '@angular/common/http';


import { CoreCliDataService } from './api/coreCliData.service';
import { CoreServerService } from './api/coreServer.service';
import { CoreServerDataService } from './api/coreServerData.service';
import { CoreServerVersionsService } from './api/coreServerVersions.service';
import { InfoService } from './api/info.service';
import { LicenseService } from './api/license.service';
import { PluginsService } from './api/plugins.service';
import { UserService } from './api/user.service';

@NgModule({
  imports:      [],
  declarations: [],
  exports:      [],
  providers: [
    CoreCliDataService,
    CoreServerService,
    CoreServerDataService,
    CoreServerVersionsService,
    InfoService,
    LicenseService,
    PluginsService,
    UserService ]
})
export class ApiModule {
    public static forRoot(configurationFactory: () => Configuration): ModuleWithProviders<ApiModule> {
        return {
            ngModule: ApiModule,
            providers: [ { provide: Configuration, useFactory: configurationFactory } ]
        };
    }

    constructor( @Optional() @SkipSelf() parentModule: ApiModule,
                 @Optional() http: HttpClient) {
        if (parentModule) {
            throw new Error('ApiModule is already loaded. Import in your base AppModule only.');
        }
        if (!http) {
            throw new Error('You need to import the HttpClientModule in your AppModule! \n' +
            'See also https://github.com/angular/angular/issues/20575');
        }
    }
}
