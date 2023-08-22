import { Injectable } from "@angular/core";
import { BaseFormService } from "../base/base-form.service";
import { v4 as uuid } from "uuid";
import { License } from "../webapi/model/license";
import { LicenseService } from "../webapi/api/license.service";
import { UserDataService } from "../shared/user-data.service";
import { GenerateLicenseData } from "../webapi/model/generateLicenseData";
import { PluginFeature } from "../webapi";

@Injectable()
export class LicenseManagementService extends BaseFormService<License> {

    public features: PluginFeature[] = [];

    constructor(public licenseService: LicenseService, private userDataService: UserDataService) {
        super();

    }

    createNew(): License {
        return {
            objId: uuid(),
            isNewObject: true, features: []
        };
    }


    delete(objId: string): Promise<License> {
        return super.call<License>(() => this.licenseService._delete(objId, "1", "response").toPromise());
    }
    save(object: License): Promise<License> {
        const generateData: GenerateLicenseData = {
            objId: object.objId,
            expires: new Date(2018, 12, 31, 23, 59, 59, 59),
            email: object.email,
            licensedTo: object.licensedTo,
            maxDatapoints: object.maxDatapoints,
            maxUsers: object.maxUsers,
            this2CoreServer: object.this2CoreServerNavigation.objId,
            features: object.features,
            allowRemoteControl: object.allowRemoteControl,
            maxRemoteTunnels: object.maxRemoteTunnels,
            maxRecordingDataPoints: object.maxRecordingDataPoints
        };

        return super.call<License>(() => this.licenseService.generateLicense("1", this.userDataService.user.apiKey, generateData, "response").toPromise());
    }
    async getAll(): Promise<License[]> {
        var features = await super.call<PluginFeature[]>(() => this.licenseService.getAllPluginFeatures("1", "response").toPromise());
        this.features = features;

        return super.call<License[]>(() => this.licenseService.getLicenses("1", "response").toPromise());
    }

}
