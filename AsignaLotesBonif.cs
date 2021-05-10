using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualD.vkBaseForm;
using VisualD.vkFormInterface;
using VisualD.GlobalVid;
using VisualD.SBOFunctions;
using VisualD.SBOGeneralService;
using SAPbobsCOM;
using SAPbouiCOM;
using System.Globalization;

namespace Bebidas
{

  public class TLotesInfo
  {
    public String Lote = "";
    public String Almacen = "";
    public Double Qty = 0.0;
  }


  class TAsignaLotesBonif : TvkBaseForm, IvkFormInterface
  {
    private int nError;
    private Form oForm;
    //private Recordset oRecordSet;
    private String sError;
    private SAPbouiCOM.Grid oGrid;
    private SAPbouiCOM.DataTable oDataTable;

    public Double MaxQty = 0.0;
    public String Articulo = "";
    public String Almacen = "";
    public List<TLotesInfo> ListLotesSel;

    private Double GetTotaSeleccionado()
    {
      Double d = 0.0;
      for (int i = 0; i <= oDataTable.Rows.Count - 1; i++)
      {
        if ((System.String)oDataTable.GetValue("Sel", i) == "Y")
          d = d + (System.Double)oDataTable.GetValue("QtySel", i);
      }
      return d;
    }

    public new bool InitForm(string uid, string xmlPath, ref Application application, ref SAPbobsCOM.Company company, ref CSBOFunctions SBOFunctions, ref TGlobalVid _GlobalSettings)
    {
      bool Result = base.InitForm(uid, xmlPath, ref application, ref company, ref SBOFunctions, ref _GlobalSettings);
      try
      {
        FSBOf.LoadForm(xmlPath, "VID_AsigLotes.srf", uid);

        oForm = FSBOApp.Forms.Item(uid);
        oForm.Freeze(true);
        oForm.AutoManaged = true;
        oForm.SupportedModes = 1;//BoAutoFormMode.afm_Ok;             // afm_All
        oForm.Mode = SAPbouiCOM.BoFormMode.fm_OK_MODE;
        oGrid = (SAPbouiCOM.Grid)oForm.Items.Item("grid_0").Specific;
        oDataTable = oForm.DataSources.DataTables.Add("DT_DATA");

        oForm.DataSources.UserDataSources.Add("Total", SAPbouiCOM.BoDataType.dt_QUANTITY, 0);
        oForm.DataSources.UserDataSources.Add("TotalSel", SAPbouiCOM.BoDataType.dt_QUANTITY, 0);
        ((SAPbouiCOM.EditText)oForm.Items.Item("Total").Specific).DataBind.SetBound(true, "", "Total");
        ((SAPbouiCOM.EditText)oForm.Items.Item("TotalSel").Specific).DataBind.SetBound(true, "", "TotalSel");

        //oRecordSet := RecordSet(FCmpny.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset));

        oForm.DataSources.UserDataSources.Item("Total").ValueEx = FSBOf.DoubleToStr(MaxQty);
        oForm.DataSources.UserDataSources.Item("TotalSel").ValueEx = FSBOf.DoubleToStr(GetTotaSeleccionado());
        oForm.Freeze(false);
        oForm.Visible = true;
        CargarGrilla();
      }
      catch (Exception e)
      {
        FCmpny.GetLastError(out nError, out sError);
        FSBOApp.StatusBar.SetText(e.Message, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
        OutLog("InitForm: " + nError.ToString() + " - " + sError + " - " + e.Message + " ** Trace: " + e.StackTrace);
      }
      return Result;
    }

    private void CargarGrilla()
    {

      String StrSQL = String.Format(@"SELECT 'N' Sel, T0.BatchNum, T0.Quantity, T0.ExpDate, T0.WhsCode, T0.Quantity QtySel FROM OIBT T0
      WHERE T0.Quantity > 0 AND T0.ItemCode = '{0}' AND T0.WhsCode = '{1}' ORDER BY T0.ExpDate ASC", Articulo, Almacen);
      oForm.Freeze(true);
      try
      {
        //oDataTable = oForm.DataSources.DataTables.Item("DT_DATA");
        oDataTable.ExecuteQuery(StrSQL);
        oGrid.DataTable = oDataTable;
        GridColumn oColumn;

        oGrid.Columns.Item("Sel").Type = BoGridColumnType.gct_CheckBox;
        oColumn = (GridColumn)oGrid.Columns.Item("Sel");
        ((CheckBoxColumn)oColumn).TitleObject.Caption = "Seleccionar";
        ((CheckBoxColumn)oColumn).Editable = true;

        oGrid.Columns.Item("BatchNum").Type = BoGridColumnType.gct_EditText;
        oColumn = (GridColumn)oGrid.Columns.Item("BatchNum");
        ((EditTextColumn)oColumn).Editable = false;
        ((EditTextColumn)oColumn).TitleObject.Caption = "Lote";

        oGrid.Columns.Item("Quantity").Type = BoGridColumnType.gct_EditText;
        oColumn = (GridColumn)oGrid.Columns.Item("Quantity");
        ((EditTextColumn)oColumn).Editable = false;
        ((EditTextColumn)oColumn).TitleObject.Caption = "Stock";
        ((EditTextColumn)oColumn).RightJustified = true;

        oGrid.Columns.Item("ExpDate").Type = BoGridColumnType.gct_EditText;
        oColumn = (GridColumn)oGrid.Columns.Item("ExpDate");
        ((EditTextColumn)oColumn).Editable = false;
        ((EditTextColumn)oColumn).TitleObject.Caption = "Fecha Vencimiento";

        oGrid.Columns.Item("WhsCode").Type = BoGridColumnType.gct_EditText;
        oColumn = (GridColumn)oGrid.Columns.Item("WhsCode");
        ((EditTextColumn)oColumn).Editable = false;
        ((EditTextColumn)oColumn).TitleObject.Caption = "Almacen";
        ((EditTextColumn)oColumn).LinkedObjectType = "64";

        oGrid.Columns.Item("QtySel").Type = BoGridColumnType.gct_EditText;
        oColumn = (GridColumn)oGrid.Columns.Item("QtySel");
        ((EditTextColumn)oColumn).Editable = true;
        ((EditTextColumn)oColumn).TitleObject.Caption = "Cantidad Asignada";
        ((EditTextColumn)oColumn).RightJustified = true;


        for (int i = 0; i <= oDataTable.Rows.Count - 1; i++)
        {
          for (int j = 0; j <= ListLotesSel.Count - 1; i++)
          {
            if ((ListLotesSel[j].Lote == (System.String)oDataTable.GetValue("BatchNum", i)) && (ListLotesSel[j].Almacen == (System.String)oDataTable.GetValue("WhsCode", i)))
            {
              oDataTable.SetValue("Sel", i, "Y");
              oDataTable.SetValue("QtySel", i, ListLotesSel[j].Qty);
              break;
            }
          }
        }
      }
      finally
      {
        oForm.Freeze(false);
      }
      oGrid.AutoResizeColumns();
    }

    public new void FormEvent(string FormUID, ref ItemEvent pVal, ref bool BubbleEvent)
    {
      base.FormEvent(FormUID, ref pVal, ref BubbleEvent);
      try
      {
        if ((pVal.EventType == SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED) && pVal.BeforeAction)
        {
          switch (pVal.ItemUID)
          {
            case "1":
              if (Validar())
              {
                GuardarSeleccion();
                oForm.Close();
              }
              else
                BubbleEvent = false;

              break;
          }
        }

        if ((pVal.EventType == SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED) && !pVal.BeforeAction)
        {
          switch (pVal.ColUID)
          {
            case "Sel":
              oForm.Freeze(true);
              oForm.DataSources.UserDataSources.Item("TotalSel").ValueEx = FSBOf.DoubleToStr(GetTotaSeleccionado());
              oForm.Freeze(false);
              break;
          }
        }

        if ((pVal.EventType == SAPbouiCOM.BoEventTypes.et_VALIDATE) && !pVal.BeforeAction)
        {
          switch (pVal.ColUID)
          {
            case "QtySel":
              oForm.Freeze(true);
              oForm.DataSources.UserDataSources.Item("TotalSel").ValueEx = FSBOf.DoubleToStr(GetTotaSeleccionado());
              oForm.Freeze(false);
              break;
          }
        }

      }
      catch (Exception e)
      {
        FCmpny.GetLastError(out nError, out sError);
        FSBOApp.StatusBar.SetText(e.Message, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
        OutLog("FormEvent: " + nError.ToString() + " - " + sError + " - " + e.Message + " ** Trace: " + e.StackTrace);
      }
    }

    private void GuardarSeleccion()
    {
      ListLotesSel.Clear();
      for (int i = 0; i <= oDataTable.Rows.Count - 1; i++)
      {
        if ((System.String)oDataTable.GetValue("Sel", i) == "Y")
        {
          TLotesInfo LoteInfo = new TLotesInfo();
          LoteInfo.Lote = (System.String)oDataTable.GetValue("BatchNum", i);
          LoteInfo.Almacen = (System.String)oDataTable.GetValue("WhsCode", i);
          LoteInfo.Qty = (System.Double)oDataTable.GetValue("QtySel", i);
          ListLotesSel.Add(LoteInfo);
        }
      }
    }

    private bool Validar()
    {
      bool b = true;
      oForm.Freeze(true);
      Double TotalSel = GetTotaSeleccionado();
      oForm.DataSources.UserDataSources.Item("TotalSel").ValueEx = FSBOf.DoubleToStr(TotalSel);
      oForm.Freeze(false);
      double difference = .0000001;
      if (Math.Abs(TotalSel - MaxQty) > difference)
      {
        b = false;
        FSBOApp.StatusBar.SetText(String.Format("La cantidad seleccionada {0} es distinta a la cantidad requerida {1}", TotalSel, MaxQty), BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
      }
      return b;
    }

  }
}
