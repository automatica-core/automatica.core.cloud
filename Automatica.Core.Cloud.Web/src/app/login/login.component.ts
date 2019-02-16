import { Component, OnInit } from "@angular/core";
import { UserService } from "../webapi/";
import { Router } from "@angular/router";
import { Language } from "angular-l10n";
import { UserDataService } from "../shared/user-data.service";

@Component({
  selector: "core-login",
  templateUrl: "./login.component.html",
  styleUrls: ["./login.component.scss"]
})
export class LoginComponent implements OnInit {

  public username: string;
  public password: string;

  @Language() lang: string;

  constructor(private userService: UserService, private router: Router, private userDataService: UserDataService) { }

  ngOnInit() {
    localStorage.removeItem("token");
  }

  async submit() {
    await this.login();
  }

  async login() {
    try {
      const ret = await this.userService.login("1", {
        username: this.username,
        password: this.password
      }).toPromise();

      if (ret) {
        this.userDataService.setUser(ret);
        localStorage.setItem("token", ret.token);
        window.location.href = "/";
      }

    } catch (error) {
      console.error(error);
    }
  }

}
