import { Component, OnInit } from "@angular/core";

@Component({
  selector: "core-server-connections-detail",
  templateUrl: "./server-connections-detail.component.html",
  styleUrls: ["./server-connections-detail.component.scss"]
})
export class ServerConnectionsDetailComponent implements OnInit {

  loading = false;
  
  constructor() { }

  ngOnInit() {
  }

}
