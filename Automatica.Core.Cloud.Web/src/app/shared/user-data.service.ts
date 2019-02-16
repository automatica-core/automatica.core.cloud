import { Injectable } from "@angular/core";
import { User } from "../webapi";

@Injectable()
export class UserDataService {
    private _user: User;

    public setUser(user: User) {
        this._user = user;
        localStorage.setItem("user", JSON.stringify(user));
    }

    public get user(): User {
        if (!this._user) {
            const userJson = localStorage.getItem("user");
            if (userJson) {
                this._user = JSON.parse(userJson);
            }
        }
        return this._user;
    }
}
