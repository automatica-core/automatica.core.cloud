import { RouterModule, Routes } from "@angular/router";
import { LayoutComponent } from "./layout.component";
import { DashboardComponent } from "../dashboard/dashboard.component";

const AppRoutes: Routes = [
    {
        path: "", component: LayoutComponent, children: [
            {
                path: "dashboard", component: DashboardComponent
            }, {
                path: "versions", loadChildren: () => import('../server-versions-management/server-versions-management.module').then(m => m.ServerVersionsManagementModule)
            }, {
                path: "plugins", loadChildren: () => import('../plugins/plugins.module').then(m => m.PluginsModule)
            }, {
                path: "servers", loadChildren: () => import('../server-connections-management/server-connections-management.module').then(m => m.ServerConnectionsManagementModule)
            }, {
                path: "licenses", loadChildren: () => import('../license-management/license-management.module').then(m => m.LicenseManagementModule)
            }
        ]
    }
];

export const LayoutRoutingModule = RouterModule.forChild(AppRoutes);
