import { Routes, RouterModule } from "@angular/router";
import { NgModule } from "@angular/core";
import { ServerConnectionsManagementComponent } from "./server-connections-management.component";

const routes: Routes = [
    {
        path: "",
        component: ServerConnectionsManagementComponent
    }
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ServerConnectionsManagementRoutingModule { }
