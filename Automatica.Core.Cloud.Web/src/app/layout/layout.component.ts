import { Component, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";

@Component({
  selector: "core-layout",
  templateUrl: "./layout.component.html",
  styleUrls: ["./layout.component.scss"]
})
export class LayoutComponent implements OnInit {

  constructor(private activatedRoute: ActivatedRoute, private router: Router) { }

  async ngOnInit() {
    const url = this.router.url;

    if (url.endsWith("/admin")) {
      await this.router.navigate(["admin", "dashboard"]);
    }
  }

}
