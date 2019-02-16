import { RouterModule, Routes } from "@angular/router";
import { LayoutComponent } from "./layout.component";
import { DashboardComponent } from "../dashboard/dashboard.component";

const AppRoutes: Routes = [
    {
        path: "", component: LayoutComponent, children: [
            {
                path: "dashboard", component: DashboardComponent
            }, {
                path: "versions", loadChildren: "../server-versions-management/server-versions-management.module#ServerVersionsManagementModule"
            }, {
                path: "plugins", loadChildren: "../plugins/plugins.module#PluginsModule"
            }, {
                path: "servers", loadChildren: "../server-connections-management/server-connections-management.module#ServerConnectionsManagementModule"
            }, {
                path: "licenses", loadChildren: "../license-management/license-management.module#LicenseManagementModule"
            }
        ]
    }
];

export const LayoutRoutingModule = RouterModule.forChild(AppRoutes);
