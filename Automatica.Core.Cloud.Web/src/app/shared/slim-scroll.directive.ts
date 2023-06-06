import { Directive, ElementRef, Input, AfterViewInit } from "@angular/core";
import "jquery-slimscroll/jquery.slimscroll.min.js";
declare var $: any;

@Directive({ selector: "[coreSlimScroll]" })
export class SlimScrollDirective implements AfterViewInit {

  @Input()
  scrollHeight: string;


  el: ElementRef;
  constructor(el: ElementRef) {
    this.el = el;
  }


  ngAfterViewInit() {
    const $el = $(this.el.nativeElement);

    ($el as any).slimScroll({
      height: this.scrollHeight || "100%"
    });
  }
}
