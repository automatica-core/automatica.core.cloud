import { Component, OnInit } from "@angular/core";
import { LicenseManagementService } from "./license-management.service";

@Component({
  selector: "core-license-management",
  templateUrl: "./license-management.component.html",
  styleUrls: ["./license-management.component.scss"]
})
export class LicenseManagementComponent implements OnInit {

  constructor(private licenseService: LicenseManagementService) { }


  ngOnInit() {

  }


}
