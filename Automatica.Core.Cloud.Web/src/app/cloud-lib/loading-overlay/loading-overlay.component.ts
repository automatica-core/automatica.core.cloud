import { Component, OnInit } from "@angular/core";
import { LoadingOverlayService } from "./loading-overlay.service";

@Component({
  selector: "core-loading-overlay",
  templateUrl: "./loading-overlay.component.html",
  styleUrls: ["./loading-overlay.component.scss"]
})
export class LoadingOverlayComponent implements OnInit {
  constructor(public loadingService: LoadingOverlayService) { }

  ngOnInit() {
  }

}
