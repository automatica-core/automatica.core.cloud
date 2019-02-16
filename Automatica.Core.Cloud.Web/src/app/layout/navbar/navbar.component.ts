import { Component, OnInit } from "@angular/core";

@Component({
  selector: "core-navbar",
  templateUrl: "./navbar.component.html",
  styleUrls: ["./navbar.component.scss"]
})
export class NavbarComponent implements OnInit {

  navCollapsed = false;
  constructor() { }

  ngOnInit() {
  }

}
