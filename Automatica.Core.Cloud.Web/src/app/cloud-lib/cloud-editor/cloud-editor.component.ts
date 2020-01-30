import { Component, OnInit, ViewChild, ElementRef, Input } from "@angular/core";

@Component({
  selector: "core-cloud-editor",
  templateUrl: "./cloud-editor.component.html",
  styleUrls: ["./cloud-editor.component.scss"]
})
export class CloudEditorComponent implements OnInit {


  @ViewChild("content", {static: true})
  template: ElementRef;

  @Input()
  title: string;

  constructor() { }

  ngOnInit() {
  }

}
