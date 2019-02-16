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
/* tslint:disable:no-unused-variable member-ordering */

import { Inject, Injectable, Optional }                      from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams,
         HttpResponse, HttpEvent }                           from '@angular/common/http';
import { CustomHttpUrlEncodingCodec }                        from '../encoder';

import { Observable }                                        from 'rxjs/Observable';

import { Plugin } from '../model/plugin';

import { BASE_PATH, COLLECTION_FORMATS }                     from '../variables';
import { Configuration }                                     from '../configuration';


@Injectable()
export class CoreCliDataService {

    protected basePath = 'https://localhost';
    public defaultHeaders = new HttpHeaders();
    public configuration = new Configuration();

    constructor(protected httpClient: HttpClient, @Optional()@Inject(BASE_PATH) basePath: string, @Optional() configuration: Configuration) {
        if (basePath) {
            this.basePath = basePath;
        }
        if (configuration) {
            this.configuration = configuration;
            this.basePath = basePath || configuration.basePath || this.basePath;
        }
    }

    /**
     * @param consumes string[] mime-types
     * @return true: consumes contains 'multipart/form-data', false: otherwise
     */
    private canConsumeForm(consumes: string[]): boolean {
        const form = 'multipart/form-data';
        for (const consume of consumes) {
            if (form === consume) {
                return true;
            }
        }
        return false;
    }


    /**
     * 
     * 
     * @param version 
     * @param apiKey 
     * @param observe set whether or not to return the data Observable as the body, response or events. defaults to returning the body.
     * @param reportProgress flag to report request and response progress.
     */
    public deploy(version: string, apiKey: string, observe?: 'body', reportProgress?: boolean): Observable<any>;
    public deploy(version: string, apiKey: string, observe?: 'response', reportProgress?: boolean): Observable<HttpResponse<any>>;
    public deploy(version: string, apiKey: string, observe?: 'events', reportProgress?: boolean): Observable<HttpEvent<any>>;
    public deploy(version: string, apiKey: string, observe: any = 'body', reportProgress: boolean = false ): Observable<any> {

        if (version === null || version === undefined) {
            throw new Error('Required parameter version was null or undefined when calling deploy.');
        }

        if (apiKey === null || apiKey === undefined) {
            throw new Error('Required parameter apiKey was null or undefined when calling deploy.');
        }

        let headers = this.defaultHeaders;

        // authentication (Bearer) required
        if (this.configuration.apiKeys["Authorization"]) {
            headers = headers.set('Authorization', this.configuration.apiKeys["Authorization"]);
        }

        // to determine the Accept header
        let httpHeaderAccepts: string[] = [
        ];
        const httpHeaderAcceptSelected: string | undefined = this.configuration.selectHeaderAccept(httpHeaderAccepts);
        if (httpHeaderAcceptSelected != undefined) {
            headers = headers.set('Accept', httpHeaderAcceptSelected);
        }

        // to determine the Content-Type header
        const consumes: string[] = [
        ];

        return this.httpClient.post<any>(`${this.basePath}/v${encodeURIComponent(String(version))}/coreCliData/deploy/${encodeURIComponent(String(apiKey))}`,
            null,
            {
                withCredentials: this.configuration.withCredentials,
                headers: headers,
                observe: observe,
                reportProgress: reportProgress
            }
        );
    }

    /**
     * 
     * 
     * @param apiKey 
     * @param version 
     * @param observe set whether or not to return the data Observable as the body, response or events. defaults to returning the body.
     * @param reportProgress flag to report request and response progress.
     */
    public deployPlugin(apiKey: string, version: string, observe?: 'body', reportProgress?: boolean): Observable<any>;
    public deployPlugin(apiKey: string, version: string, observe?: 'response', reportProgress?: boolean): Observable<HttpResponse<any>>;
    public deployPlugin(apiKey: string, version: string, observe?: 'events', reportProgress?: boolean): Observable<HttpEvent<any>>;
    public deployPlugin(apiKey: string, version: string, observe: any = 'body', reportProgress: boolean = false ): Observable<any> {

        if (apiKey === null || apiKey === undefined) {
            throw new Error('Required parameter apiKey was null or undefined when calling deployPlugin.');
        }

        if (version === null || version === undefined) {
            throw new Error('Required parameter version was null or undefined when calling deployPlugin.');
        }

        let headers = this.defaultHeaders;

        // authentication (Bearer) required
        if (this.configuration.apiKeys["Authorization"]) {
            headers = headers.set('Authorization', this.configuration.apiKeys["Authorization"]);
        }

        // to determine the Accept header
        let httpHeaderAccepts: string[] = [
        ];
        const httpHeaderAcceptSelected: string | undefined = this.configuration.selectHeaderAccept(httpHeaderAccepts);
        if (httpHeaderAcceptSelected != undefined) {
            headers = headers.set('Accept', httpHeaderAcceptSelected);
        }

        // to determine the Content-Type header
        const consumes: string[] = [
        ];

        return this.httpClient.post<any>(`${this.basePath}/v${encodeURIComponent(String(version))}/coreCliData/deployPlugin/${encodeURIComponent(String(apiKey))}`,
            null,
            {
                withCredentials: this.configuration.withCredentials,
                headers: headers,
                observe: observe,
                reportProgress: reportProgress
            }
        );
    }

    /**
     * 
     * 
     * @param apiKey 
     * @param deleteOldPackages 
     * @param version 
     * @param observe set whether or not to return the data Observable as the body, response or events. defaults to returning the body.
     * @param reportProgress flag to report request and response progress.
     */
    public deployPluginAndDelete(apiKey: string, deleteOldPackages: boolean, version: string, observe?: 'body', reportProgress?: boolean): Observable<any>;
    public deployPluginAndDelete(apiKey: string, deleteOldPackages: boolean, version: string, observe?: 'response', reportProgress?: boolean): Observable<HttpResponse<any>>;
    public deployPluginAndDelete(apiKey: string, deleteOldPackages: boolean, version: string, observe?: 'events', reportProgress?: boolean): Observable<HttpEvent<any>>;
    public deployPluginAndDelete(apiKey: string, deleteOldPackages: boolean, version: string, observe: any = 'body', reportProgress: boolean = false ): Observable<any> {

        if (apiKey === null || apiKey === undefined) {
            throw new Error('Required parameter apiKey was null or undefined when calling deployPluginAndDelete.');
        }

        if (deleteOldPackages === null || deleteOldPackages === undefined) {
            throw new Error('Required parameter deleteOldPackages was null or undefined when calling deployPluginAndDelete.');
        }

        if (version === null || version === undefined) {
            throw new Error('Required parameter version was null or undefined when calling deployPluginAndDelete.');
        }

        let headers = this.defaultHeaders;

        // authentication (Bearer) required
        if (this.configuration.apiKeys["Authorization"]) {
            headers = headers.set('Authorization', this.configuration.apiKeys["Authorization"]);
        }

        // to determine the Accept header
        let httpHeaderAccepts: string[] = [
        ];
        const httpHeaderAcceptSelected: string | undefined = this.configuration.selectHeaderAccept(httpHeaderAccepts);
        if (httpHeaderAcceptSelected != undefined) {
            headers = headers.set('Accept', httpHeaderAcceptSelected);
        }

        // to determine the Content-Type header
        const consumes: string[] = [
        ];

        return this.httpClient.post<any>(`${this.basePath}/v${encodeURIComponent(String(version))}/coreCliData/deployPlugin/${encodeURIComponent(String(deleteOldPackages))}/${encodeURIComponent(String(apiKey))}`,
            null,
            {
                withCredentials: this.configuration.withCredentials,
                headers: headers,
                observe: observe,
                reportProgress: reportProgress
            }
        );
    }

    /**
     * 
     * 
     * @param coreServerVersion 
     * @param version 
     * @param apiKey 
     * @param observe set whether or not to return the data Observable as the body, response or events. defaults to returning the body.
     * @param reportProgress flag to report request and response progress.
     */
    public getAvailablePlugins(coreServerVersion: string, version: string, apiKey: string, observe?: 'body', reportProgress?: boolean): Observable<Array<Plugin>>;
    public getAvailablePlugins(coreServerVersion: string, version: string, apiKey: string, observe?: 'response', reportProgress?: boolean): Observable<HttpResponse<Array<Plugin>>>;
    public getAvailablePlugins(coreServerVersion: string, version: string, apiKey: string, observe?: 'events', reportProgress?: boolean): Observable<HttpEvent<Array<Plugin>>>;
    public getAvailablePlugins(coreServerVersion: string, version: string, apiKey: string, observe: any = 'body', reportProgress: boolean = false ): Observable<any> {

        if (coreServerVersion === null || coreServerVersion === undefined) {
            throw new Error('Required parameter coreServerVersion was null or undefined when calling getAvailablePlugins.');
        }

        if (version === null || version === undefined) {
            throw new Error('Required parameter version was null or undefined when calling getAvailablePlugins.');
        }

        if (apiKey === null || apiKey === undefined) {
            throw new Error('Required parameter apiKey was null or undefined when calling getAvailablePlugins.');
        }

        let headers = this.defaultHeaders;

        // authentication (Bearer) required
        if (this.configuration.apiKeys["Authorization"]) {
            headers = headers.set('Authorization', this.configuration.apiKeys["Authorization"]);
        }

        // to determine the Accept header
        let httpHeaderAccepts: string[] = [
            'text/plain',
            'application/json',
            'text/json'
        ];
        const httpHeaderAcceptSelected: string | undefined = this.configuration.selectHeaderAccept(httpHeaderAccepts);
        if (httpHeaderAcceptSelected != undefined) {
            headers = headers.set('Accept', httpHeaderAcceptSelected);
        }

        // to determine the Content-Type header
        const consumes: string[] = [
        ];

        return this.httpClient.get<Array<Plugin>>(`${this.basePath}/v${encodeURIComponent(String(version))}/coreCliData/plugins/${encodeURIComponent(String(coreServerVersion))}/${encodeURIComponent(String(apiKey))}`,
            {
                withCredentials: this.configuration.withCredentials,
                headers: headers,
                observe: observe,
                reportProgress: reportProgress
            }
        );
    }

}
