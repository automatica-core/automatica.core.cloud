import { Component, OnInit } from "@angular/core";
import { InfoService } from "../webapi";

@Component({
  selector: "core-dashboard",
  templateUrl: "./dashboard.component.html",
  styleUrls: ["./dashboard.component.scss"]
})
export class DashboardComponent implements OnInit {

  constructor(private infoService: InfoService) { }

  async ngOnInit() {

  }

}
