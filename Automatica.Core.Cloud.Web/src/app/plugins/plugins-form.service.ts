import { Injectable } from "@angular/core";
import { Plugin, PluginsService } from "../webapi";
import { BaseFormService } from "../base/base-form.service";
import { v4 as uuid } from "uuid";

@Injectable()
export class PluginsFormService extends BaseFormService<Plugin> {

    public versions: Plugin[] = [];

    constructor(private pluginService: PluginsService) {
        super();

    }

    public get dataSource(): any[] {
        return this.versions;
    }
    public set dataSource(v: any[]) {
        this.versions = v;
    }

    createNew() {
        return {
            objId: uuid(),
            isNewObject: true
        };
    }
    delete(objId: string): Promise<any> {
        try {
            return super.call<any[]>(() => this.pluginService._delete("1", objId, "response").toPromise());
        } catch (error) {
            this.handleError(error);
        }
    }
    save(object: any): Promise<any> {
        try {
            return super.call<Plugin>(() => this.pluginService.save("1", object, "response").toPromise());
        } catch (error) {
            this.handleError(error);
        }
    }
    async getAll(): Promise<any[]> {
        return super.call<any[]>(() => this.pluginService.getVersions("1", "response").toPromise());
    }

    public get title() {
        return "COMMON.PLUGINS.TITLE";
    }
}
