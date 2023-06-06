import { Directive, ElementRef, OnInit } from "@angular/core";
import { Router, NavigationEnd } from "@angular/router";
declare var $: any;

// Mobile only: automatically close sidebar on route change.
@Directive({ selector: "[coreAutoCloseMobileNav]" })

export class AutoCloseMobileNavDirective implements OnInit {
  el: ElementRef;
  router: Router;
  constructor(el: ElementRef, router: Router) {
    this.el = el;
    this.router = router;
  }

  ngOnInit() {
    const $body = $("#body");
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        setTimeout(() => {
          // console.log('NavigationEnd:', event);
          $body.removeClass("sidebar-mobile-open");
        }, 0);
      }
    });
  }
}
