import { Component, OnInit, Input, Output, EventEmitter } from "@angular/core";
import { BaseFormService } from "src/app/base/base-form.service";
import { LoadingOverlayService } from "../loading-overlay/loading-overlay.service";

@Component({
  selector: "core-cloud-list",
  templateUrl: "./cloud-list.component.html",
  styleUrls: ["./cloud-list.component.scss"]
})
export class CloudListComponent implements OnInit {

  @Input()
  columns: any;

  private _item: any;

  @Input()
  public get item(): any {
    return this._item;
  }
  public set item(v: any) {
    this._item = v;
    this.itemChange.emit(v);
  }

  @Output()
  itemChange = new EventEmitter<any>();

  @Input()
  showUploadButton = false;

  @Output()
  onUpload = new EventEmitter<any>();

  constructor(public baseFormService: BaseFormService<any>, private loadingService: LoadingOverlayService) { }

  async ngOnInit() {
    await this.refresh(void 0);
  }

  async refresh($event) {
    this.loadingService.showLoadingPanel("COMMON.LOADING");

    this.baseFormService.selectedItem = void 0;
    this.item = void 0;
    this.baseFormService.selectedRows = [];

    try {
      this.baseFormService.dataSource = await this.baseFormService.getAll();
    }
    finally {
      this.loadingService.hideLoadingPanel();
    }
  }

  async add() {
    const newVersion = this.baseFormService.createNew();

    this.baseFormService.dataSource = [...this.baseFormService.dataSource, newVersion];
    this.baseFormService.selectedRows = [newVersion.objId];
  }
  async delete() {
    const objId = this.item.objId;
    try {
      await this.baseFormService.delete(objId);
    } finally {
      this.baseFormService.dataSource = this.baseFormService.dataSource.filter(a => a.objId !== objId);
      this.baseFormService.selectedRows = this.baseFormService.selectedRows.filter(a => a !== objId);
    }
  }


  selectionChanged($event) {
    if ($event.selectedRowsData && $event.selectedRowsData.length > 0) {
      this.item = $event.selectedRowsData[0];
    } else {
      this.item = void 0;
    }
    this.baseFormService.selectedItem = this.item;
  }

  upload($event) {
    this.onUpload.emit();
  }
}
