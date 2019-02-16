import { Component, OnInit } from "@angular/core";
import { ServerConnectionsService } from "./server-connections.service";

@Component({
  selector: "core-server-connections-management",
  templateUrl: "./server-connections-management.component.html",
  styleUrls: ["./server-connections-management.component.scss"]
})
export class ServerConnectionsManagementComponent implements OnInit {

  constructor(private service: ServerConnectionsService) { }

  ngOnInit() {

  }

}
