import { Component, OnInit } from "@angular/core";
import { PluginsFormService } from "../plugins-form.service";

@Component({
  selector: "core-plugin-detail",
  templateUrl: "./plugin-detail.component.html",
  styleUrls: ["./plugin-detail.component.scss"]
})
export class PluginDetailComponent implements OnInit {
  loading: boolean = false;

  pluginTypeData = [{
    name: "Driver",
    value: 0
  }, {
    name: "Rule",
    value: 1
  }];

  get uploadHeader() {
    return { "Authorization": "Bearer " + localStorage.getItem("token") };
  }

  get uploadUrl() {
    return "/webapi/v1/plugins/" + this.service.selectedItem.objId + "/upload";
  }

  constructor(private service: PluginsFormService) { }

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
