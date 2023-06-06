import { Component, OnInit, Input, Output, EventEmitter, ContentChild, TemplateRef, inject } from "@angular/core";
import { BaseFormService } from "src/app/base/base-form.service";
import { LoadingOverlayService } from "../loading-overlay/loading-overlay.service";
import { L10N_LOCALE } from "angular-l10n";


@Component({
  selector: "core-cloud-form",
  templateUrl: "./cloud-form.component.html",
  styleUrls: ["./cloud-form.component.scss"]
})
export class CloudFormComponent implements OnInit {


  locale = inject(L10N_LOCALE);

  @ContentChild(TemplateRef, { static: true })
  template: TemplateRef<any>;

  @Input()
  name: string;

  private _loading: boolean;
  @Input()
  public get loading(): boolean {
    return this._loading;
  }
  public set loading(v: boolean) {
    this._loading = v;
    if (v) {
      this.loadingService.showLoadingPanel("COMMON.LOADING");
    } else {
      this.loadingService.hideLoadingPanel();
    }
  }


  constructor(public baseFormService: BaseFormService<any>, public loadingService: LoadingOverlayService) { }

  ngOnInit() {
  }


  abort($event) {
    this.baseFormService.dataSource = this.baseFormService.dataSource.filter(a => a.objId !== this.baseFormService.selectedItem.objId);
  }

  async saveClick() {
    this.baseFormService.selectedItem = await this.baseFormService.save(this.baseFormService.selectedItem);
    this.baseFormService.selectedItem.isNewObject = false;
  }
}
