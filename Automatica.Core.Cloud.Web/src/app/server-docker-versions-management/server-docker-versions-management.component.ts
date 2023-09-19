import { Component, OnInit, } from "@angular/core";
import { CoreServerVersionsService } from "../webapi";
import { ServerDockerVersionService } from "./server-docker-versions.service";

@Component({
  selector: "core-server-docker-versions-management",
  templateUrl: "./server-docker-versions-management.component.html",
  styleUrls: ["./server-docker-versions-management.component.scss"]
})
export class ServerDockerVersionsManagementComponent implements OnInit {

  constructor(private serverVersion: CoreServerVersionsService, public service: ServerDockerVersionService) { }

  ngOnInit() {

  }
}
