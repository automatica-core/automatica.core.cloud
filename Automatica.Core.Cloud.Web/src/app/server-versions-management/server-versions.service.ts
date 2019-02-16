import { Injectable } from "@angular/core";
import { ServerVersion, CoreServerVersionsService } from "../webapi";
import { BaseFormService } from "../base/base-form.service";
import { v4 as uuid } from "uuid";

@Injectable()
export class ServerVersionService extends BaseFormService<ServerVersion> {

    public versions: ServerVersion[] = [];

    constructor(private coreServerVersionsService: CoreServerVersionsService) {
        super();

    }

    public get dataSource(): any[] {
        return this.versions;
    }
    public set dataSource(v: any[]) {
        this.versions = v;
    }

    createNew(): ServerVersion {
        return {
            objId: uuid(),
            isNewObject: true,
            isPrerelease: false,
            isPublic: true
        };
    }
    delete(objId: string): Promise<any> {
        return super.call<any>(() => this.coreServerVersionsService._delete("1", objId, "response").toPromise());
    }
    save(object: any): Promise<any> {
        return super.call<any>(() =>  this.coreServerVersionsService.save("1", object, "response").toPromise());
    }
    getAll(): Promise<any[]> {
        return super.call<any[]>(() =>  this.coreServerVersionsService.getVersions("1", "response").toPromise());
    }

    public get title() {
        return "COMMON.VERSIONS.TITLE";
    }
}
