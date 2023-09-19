import { Injectable } from "@angular/core";
import { BaseFormService } from "../base/base-form.service";
import { v4 as uuid } from "uuid";
import { ServerDockerVersion } from "../webapi/model/serverDockerVersion";
import { CoreServerDockerVersionsService } from "../webapi/api/coreServerDockerVersions.service";

@Injectable()
export class ServerDockerVersionService extends BaseFormService<ServerDockerVersion> {

    public versions: ServerDockerVersion[] = [];

    constructor(private coreServerVersionsService: CoreServerDockerVersionsService) {
        super();

    }

    public get dataSource(): any[] {
        return this.versions;
    }
    public set dataSource(v: any[]) {
        this.versions = v;
    }

    createNew(): ServerDockerVersion {
        return {
            objId: uuid(),
            isNewObject: true,
            isPreRelease: false,
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
        return "COMMON.DOCKER_VERSIONS.TITLE";
    }
}
