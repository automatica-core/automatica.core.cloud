import { RouterModule, Routes } from "@angular/router";
import { LoginComponent } from "./login/login.component";
import { AuthGuard } from "./shared/auth.service";
import { SignUpComponent } from "./sign-up/sign-up.component";
import { ResetPasswordComponent } from "./reset-password/reset-password.component";

const AppRoutes: Routes = [
    { path: "", pathMatch: "full", redirectTo: "admin" },
    {
        path: "admin", loadChildren: () => import('./layout/layout.module').then(m => m.LayoutModule), canActivate: [AuthGuard]
    }, {
        path: "login", component: LoginComponent
    }, {
        path: "signup", component: SignUpComponent
    }, {
        path: "resetpassword", component: ResetPasswordComponent
    }
];

export const AppRoutingModule = RouterModule.forRoot(AppRoutes);
