import { Routes, RouterModule } from "@angular/router";
import { NgModule } from "@angular/core";
import { ServerVersionsManagementComponent } from "./server-versions-management.component";

const routes: Routes = [
    {
        path: "",
        component: ServerVersionsManagementComponent
    }
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ServerVersionsManagementRoutingModule { }
