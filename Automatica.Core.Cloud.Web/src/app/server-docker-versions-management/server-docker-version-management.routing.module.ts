import { Routes, RouterModule } from "@angular/router";
import { NgModule } from "@angular/core";
import { ServerDockerVersionsManagementComponent } from "./server-docker-versions-management.component";

const routes: Routes = [
    {
        path: "",
        component: ServerDockerVersionsManagementComponent
    }
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ServerDockerVersionsManagementRoutingModule { }
