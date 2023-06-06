import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { LayoutComponent } from "./layout.component";
import { NavbarComponent } from "./navbar/navbar.component";
import { HeaderComponent } from "./header/header.component";
import { FooterComponent } from "./footer/footer.component";
import { LayoutRoutingModule } from "./layout.routing.module";
import { AccordionNavDirective } from "./navbar-menu/accordion-nav.directive";
import { AppendSubmenuIconDirective } from "./navbar-menu/append-submenu-icon.directive";
import { HighlightActiveItemsDirective } from "./navbar-menu/highlight-active-items.directive";
import { NavbarMenuComponent } from "./navbar-menu/navbar-menu.component";
import { AutoCloseMobileNavDirective } from "./navbar/auto-close-mobile-nav.directive";
import { ToggleOffcanvasNavDirective } from "./header/toggle-offcanvas-nav.directive";
import { SharedModule } from "../shared/shared.module";

import { MatButtonModule } from '@angular/material/button';
import { DashboardModule } from "../dashboard/dashboard.module";
import { RouterModule } from "@angular/router";

@NgModule({
  imports: [
    CommonModule,
    LayoutRoutingModule,
    SharedModule,
    MatButtonModule,
    DashboardModule,
    RouterModule
  ],
  declarations: [
    LayoutComponent,
    NavbarComponent,
    HeaderComponent,
    FooterComponent,
    AccordionNavDirective,
    AppendSubmenuIconDirective,
    HighlightActiveItemsDirective,
    NavbarMenuComponent,
    AutoCloseMobileNavDirective,
    ToggleOffcanvasNavDirective
  ]
})
export class LayoutModule { }
