import { Component, OnInit, ViewChild } from "@angular/core";
import { ServerConnectionsService } from "src/app/server-connections-management/server-connections.service";
import { CoreServer } from "src/app/webapi/model/coreServer";
import { CloudFormComponent } from "src/app/cloud-lib/cloud-form/cloud-form.component";
import { LicenseManagementService } from "../license-management.service";

@Component({
  selector: "core-license-detail",
  templateUrl: "./license-detail.component.html",
  styleUrls: ["./license-detail.component.scss"]
})
export class LicenseDetailComponent implements OnInit {

  servers: CoreServer[] = [];
  public loading = false;

  get selectedFeatures() {
    return this.licenseService.selectedItem.features;
  }

  set selectedFeatures(value: string[]) {
    this.licenseService.selectedItem.features = value;
  }

  constructor(private coreServerService: ServerConnectionsService, public licenseService: LicenseManagementService) { }

  async ngOnInit() {
    this.servers = await this.coreServerService.getAll();
  }

  serverChaned($event) {
    const server = this.servers.filter(a => a.objId === $event.value);
    this.licenseService.selectedItem.this2CoreServerNavigation = server[0];
  }
}
