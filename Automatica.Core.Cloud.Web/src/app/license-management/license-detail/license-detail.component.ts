import { Component, OnInit, ViewChild } from "@angular/core";
import { ServerConnectionsService } from "src/app/server-connections-management/server-connections.service";
import { CoreServer } from "src/app/webapi/model/coreServer";
import { CloudFormComponent } from "src/app/cloud-lib/cloud-form/cloud-form.component";
import { LicenseManagementService } from "../license-management.service";
import { License } from "src/app/webapi";

@Component({
  selector: "core-license-detail",
  templateUrl: "./license-detail.component.html",
  styleUrls: ["./license-detail.component.scss"]
})
export class LicenseDetailComponent implements OnInit {

  servers: CoreServer[] = [];
  public loading = false;
  private allLicenses: License[];

  get selectedFeatures() {
    return this.licenseService.selectedItem.features;
  }

  set selectedFeatures(value: string[]) {
    this.licenseService.selectedItem.features = value;
  }

  constructor(private coreServerService: ServerConnectionsService, public licenseService: LicenseManagementService) { }

  async ngOnInit() {
    this.servers = await this.coreServerService.getAll();
    this.allLicenses = await this.licenseService.getAll();
  }

  serverChanged($event) {

    let existingLicense = this.allLicenses.filter(a => a.this2CoreServer === $event.value);

    if (existingLicense.length > 0) {
      this.licenseService.selectedItem = this.licenseService.copyFrom(existingLicense[0]);
      this.licenseService.selectedItem.features = existingLicense[0].features;
    }
    const server = this.servers.filter(a => a.objId === $event.value);
    this.licenseService.selectedItem.this2CoreServerNavigation = server[0];
  }
}
