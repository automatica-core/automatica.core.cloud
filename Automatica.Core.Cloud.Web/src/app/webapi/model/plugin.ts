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
import { PluginFeature } from './pluginFeature';
import { User } from './user';
import { Version } from './version';


export interface Plugin { 
    objId?: string;
    pluginGuid?: string;
    name?: string;
    version?: string;
    pluginType?: Plugin.PluginTypeEnum;
    this2User?: string;
    publisher?: string;
    componentName?: string;
    pluginVersion?: string;
    readonly pluginVersionObj?: Version;
    readonly versionObj?: Version;
    minCoreServerVersion?: string;
    readonly minCoreServerVersionObj?: Version;
    azureUrl?: string;
    azureFileName?: string;
    isPublic?: boolean;
    isPrerelease?: boolean;
    readonly this2UserNavigation?: User;
    licenseFeatures?: Array<PluginFeature>;
    isNewObject?: boolean;
    branch?: string;
}
export namespace Plugin {
    export type PluginTypeEnum = 0 | 1;
    export const PluginTypeEnum = {
        NUMBER_0: 0 as PluginTypeEnum,
        NUMBER_1: 1 as PluginTypeEnum
    };
}
