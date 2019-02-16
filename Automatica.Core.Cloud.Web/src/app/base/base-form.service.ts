import { Injectable, Output, EventEmitter } from "@angular/core";
import { HttpResponse } from "@angular/common/http";

@Injectable()
export abstract class BaseFormService<T> {
  private _dataSource: any[] = [];

  private _selectedItem: T;

  public get selectedItem(): T {
    return this._selectedItem;
  }
  public set selectedItem(v: T) {
    this._selectedItem = v;
    this.selectedItemChange.emit(v);
  }

  @Output()
  selectedItemChange = new EventEmitter<T>();


  public get dataSource(): any[] {
    return this._dataSource;
  }
  public set dataSource(v: any[]) {
    this._dataSource = v;
  }


  private _selectedRows: any[];
  public get selectedRows(): any[] {
    return this._selectedRows;
  }
  public set selectedRows(v: any[]) {
    this._selectedRows = v;
  }

  public get title() {
    return "";
  }


  protected async call<T2>(callback: () => Promise<HttpResponse<T2>>): Promise<T2> {
    try {
      const retValue = await callback();

      if (retValue.ok) {
        return retValue.body;
      }
      return void 0;
    } catch (error) {
      this.handleError(error);
    }
  }

  handleError(error) {

    if (error.status === 401) {
      localStorage.clear();
      window.location.reload();
    }
  }



  abstract createNew(): T;

  abstract delete(objId: string): Promise<T>;
  abstract save(object: T): Promise<T>;
  abstract getAll(): Promise<T[]>;


}
