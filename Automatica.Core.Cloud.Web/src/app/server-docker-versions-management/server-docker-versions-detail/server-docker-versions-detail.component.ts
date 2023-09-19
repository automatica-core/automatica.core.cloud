import { Component, OnInit, Input } from "@angular/core";
import { CoreServerVersionsService } from "src/app/webapi";
import { ServerDockerVersionService } from "../server-docker-versions.service";
import { CoreServerDockerVersionsService } from "src/app/webapi/api/coreServerDockerVersions.service";

@Component({
  selector: "core-server-docker-versions-detail",
  templateUrl: "./server-docker-versions-detail.component.html",
  styleUrls: ["./server-docker-versions-detail.component.scss"]
})
export class ServerDockerVersionsDetailComponent implements OnInit {

  loading: boolean = false;


  get uploadHeader() {
    return { "Authorization": "Bearer " + localStorage.getItem("token") };
  }

  get uploadUrl() {
    return "/webapi/v1/coreServerVersion/" + this.service.selectedItem.objId + "/upload";
  }


  constructor(private service: ServerDockerVersionService, private serverVersion: CoreServerDockerVersionsService) { }

  ngOnInit() {
  }

}
