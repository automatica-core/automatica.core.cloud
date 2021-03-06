import { Routes, RouterModule } from "@angular/router";
import { NgModule } from "@angular/core";
import { PluginsComponent } from "./plugins.component";

const routes: Routes = [
    {
        path: "",
        component: PluginsComponent
    }
];
@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class PluginsRoutingModule { }
