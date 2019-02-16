import { Injectable } from "@angular/core";
import { BaseFormService } from "../base/base-form.service";
import { CoreServer } from "../webapi/model/coreServer";
import { v4 as uuid } from "uuid";
import { CoreServerService } from "../webapi";

@Injectable()
export class ServerConnectionsService extends BaseFormService<CoreServer> {

    constructor(public coreServerService: CoreServerService) {
        super();

    }

    createNew(): CoreServer {
        return {
            objId: uuid(),
            isNewObject: true
        };
    }
    delete(objId: string): Promise<CoreServer> {
        return super.call<CoreServer>(() =>  this.coreServerService._delete("1", objId, "response").toPromise());
    }
    save(object: CoreServer): Promise<CoreServer> {
        return super.call<CoreServer>(() =>  this.coreServerService.save("1", object, "response").toPromise());
    }
    getAll(): Promise<CoreServer[]> {
        return super.call<CoreServer[]>(() =>  this.coreServerService.getServers("1", "response").toPromise());
    }

}
