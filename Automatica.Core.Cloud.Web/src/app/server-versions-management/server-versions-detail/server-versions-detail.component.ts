import { Component, OnInit, Input } from "@angular/core";
import { CoreServerVersionsService } from "src/app/webapi";
import { ServerVersionService } from "../server-versions.service";

@Component({
  selector: "core-server-versions-detail",
  templateUrl: "./server-versions-detail.component.html",
  styleUrls: ["./server-versions-detail.component.scss"]
})
export class ServerVersionsDetailComponent implements OnInit {

  loading: boolean = false;


  get uploadHeader() {
    return { "Authorization": "Bearer " + localStorage.getItem("token") };
  }

  get uploadUrl() {
    return "/webapi/v1/coreServerVersion/" + this.service.selectedItem.objId + "/upload";
  }

  ridData = [{
    name: "Linux ARM32",
    value: "linux-arm"
  }, {
    name: "Windows x86",
    value: "win-x86"
  }, {
    name: "Windows x64",
    value: "win-x64"
  }];


  constructor(private service: ServerVersionService, private serverVersion: CoreServerVersionsService) { }

  ngOnInit() {
  }

  onUploadError() {
    this.loading = false;
  }

  onUploadDone() {
    this.loading = false;
  }

  uploadStarted() {
    this.loading = true;
  }

  download($event) {
    window.open(this.service.selectedItem.azureUrl);
  }

}
