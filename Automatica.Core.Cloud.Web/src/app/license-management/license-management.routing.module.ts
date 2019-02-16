import { Routes, RouterModule } from "@angular/router";
import { NgModule } from "@angular/core";
import { LicenseManagementComponent } from "./license-management.component";

const routes: Routes = [
    {
        path: "",
        component: LicenseManagementComponent
    }
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class LicenseManagementRoutingModule { }
