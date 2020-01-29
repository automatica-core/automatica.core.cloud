import { Component, OnInit, ViewChild } from "@angular/core";
import { PluginsService } from "../webapi";
import { PluginsFormService } from "./plugins-form.service";
import { LoadingOverlayService } from "../cloud-lib/loading-overlay/loading-overlay.service";
import { CloudListComponent } from "../cloud-lib/cloud-list/cloud-list.component";

@Component({
  selector: "core-plugins",
  templateUrl: "./plugins.component.html",
  styleUrls: ["./plugins.component.scss"]
})
export class PluginsComponent implements OnInit {

  get uploadHeader() {
    return { "Authorization": "Bearer " + localStorage.getItem("token") };
  }

  get uploadUrl() {
    return "/webapi/v1/plugins/upload";
  }

  @ViewChild("cloudList", {static: true})
  cloudList: CloudListComponent;

  popupVisible = false;
  constructor(private serverVersion: PluginsService, public service: PluginsFormService, private loadingService: LoadingOverlayService) { }


  ngOnInit() {

  }

  onUploadError() {
    this.loadingService.hideLoadingPanel();
    this.popupVisible = false;
  }

  onUploadDone() {
    this.loadingService.hideLoadingPanel();
    this.popupVisible = false;

    this.cloudList.refresh(void 0);
  }

  uploadStarted() {
    this.loadingService.showLoadingPanel("COMMON.LOADING");
  }

  upload($event) {
    this.popupVisible = true;
  }

}
