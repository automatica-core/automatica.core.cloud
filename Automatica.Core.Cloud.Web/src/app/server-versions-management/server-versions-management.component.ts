import { Component, OnInit, ViewChild } from "@angular/core";
import { CoreServerVersionsService } from "../webapi";
import { DxDataGridComponent } from "devextreme-angular";
import { ServerVersionService } from "./server-versions.service";
import { v4 as uuid } from "uuid";

@Component({
  selector: "core-server-versions-management",
  templateUrl: "./server-versions-management.component.html",
  styleUrls: ["./server-versions-management.component.scss"]
})
export class ServerVersionsManagementComponent implements OnInit {

  constructor(private serverVersion: CoreServerVersionsService, public service: ServerVersionService) { }

  ngOnInit() {

  }
}
