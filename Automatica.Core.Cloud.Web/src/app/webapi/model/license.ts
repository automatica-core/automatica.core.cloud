/**
 * Automatica.Core.Cloud
 * No description provided (generated by Swagger Codegen https://github.com/swagger-api/swagger-codegen)
 *
 * OpenAPI spec version: v1
 * 
 *
 * NOTE: This class is auto generated by the swagger code generator program.
 * https://github.com/swagger-api/swagger-codegen.git
 * Do not edit the class manually.
 */
import { CoreServer } from './coreServer';
import { LicenseKey } from './licenseKey';


export interface License {
    objId?: string;
    this2CoreServer?: string;
    licenseKey?: string;
    this2VersionKey?: string;
    maxDatapoints?: number;
    maxUsers?: number;
    licensedTo?: string;
    email?: string;
    this2CoreServerNavigation?: CoreServer;
    this2VersionKeyNavigation?: LicenseKey;
    featuresString?: string;
    isNewObject?: boolean;
    allowRemoteControl?: boolean;
    maxRemoteTunnels?: number;

    features: string[];
}
