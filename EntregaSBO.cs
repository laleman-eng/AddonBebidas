using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualD.vkBaseForm;
using VisualD.vkFormInterface;
using VisualD.GlobalVid;
using VisualD.SBOFunctions;
using VisualD.SBOGeneralService;
using VisualD.MultiFunctions;
using SAPbobsCOM;
using SAPbouiCOM;
using System.Globalization;

namespace Bebidas
{
  class TEntregaSBO : TvkBaseForm, IvkFormInterface
  {
    private int nError;
    private Form oForm;
    private Recordset oRecordSet;
    private String sError;
    private SAPbouiCOM.Matrix oMatrix;
    private Dictionary<String, List<TLotesInfo>> ListLotesSelBonif1 = new Dictionary<String, List<TLotesInfo>>();
    private Dictionary<String, List<TLotesInfo>> ListLotesSelBonif2 = new Dictionary<String, List<TLotesInfo>>();
    private Dictionary<String, String> LastValues = new Dictionary<String, String>();

    //no hay validacion entre lotes de SBO y lotes seleccionados en ventana local
    //no detecta cambios en la cantidad, guardar cantidad ---
    private Boolean Bonif1Checked(Int32 RowNumber)
    {
      String Bonif1;
      try
      {
        Bonif1 = ((SAPbouiCOM.ComboBox)oMatrix.Columns.Item("U_VID_Bonif1").Cells.Item(RowNumber).Specific).Selected.Value;
      }
      catch
      {
        Bonif1 = String.Empty;
      }
      return (Bonif1 == "Y");
    }

    private Boolean Bonif2Checked(Int32 RowNumber)
    {
      String Bonif2;
      try
      {
        Bonif2 = ((SAPbouiCOM.ComboBox)oMatrix.Columns.Item("U_VID_Bonif2").Cells.Item(RowNumber).Specific).Selected.Value;
      }
      catch
      {
        Bonif2 = String.Empty;
      }
      return (Bonif2 == "Y");

    }

    public new bool InitForm(string uid, string xmlPath, ref Application application, ref SAPbobsCOM.Company company, ref CSBOFunctions SBOFunctions, ref TGlobalVid _GlobalSettings)
    {
      bool Result = base.InitForm(uid, xmlPath, ref application, ref company, ref SBOFunctions, ref _GlobalSettings);
      try
      {
        oForm = FSBOApp.Forms.Item(uid);
        oRecordSet = FCmpny.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
        //oDBDS = oForm.DataSources.DBDataSources.Item("ODLN");
        oMatrix = (SAPbouiCOM.Matrix)oForm.Items.Item("38").Specific;
        String s = "2";
        SAPbouiCOM.Item vItem = oForm.Items.Item(s);
        SAPbouiCOM.Item oItem = oForm.Items.Add("btn_AsigL1", BoFormItemTypes.it_BUTTON);
        oItem.Width = 130;
        oItem.Top = vItem.Top;
        oItem.Height = vItem.Height;
        oItem.Left = vItem.Left + vItem.Width + 3;
        oItem.LinkTo = "2";
        ((SAPbouiCOM.Button)oItem.Specific).Type = BoButtonTypes.bt_Caption;
        ((SAPbouiCOM.Button)oItem.Specific).Caption = "Asignar Lotes Bonif. 1";

        vItem = oForm.Items.Item("btn_AsigL1");
        oItem = oForm.Items.Add("btn_AsigL2", BoFormItemTypes.it_BUTTON);
        oItem.Width = 130;
        oItem.Top = vItem.Top;
        oItem.Height = vItem.Height;
        oItem.Left = vItem.Left + vItem.Width + 3;
        oItem.LinkTo = "2";
        ((SAPbouiCOM.Button)oItem.Specific).Type = BoButtonTypes.bt_Caption;
        ((SAPbouiCOM.Button)oItem.Specific).Caption = "Asignar Lotes Bonif. 2";

        List<String> Lista = new List<String>();
        // Ok Ad  Fnd Vw Rq Sec
        Lista.Add("btn_AsigL1  , f,  t,  f,  f, n, 1");
        Lista.Add("btn_AsigL2  , f,  t,  f,  f, n, 1");
        FSBOf.SetAutoManaged(ref oForm, Lista);
      }
      catch (Exception e)
      {
        FCmpny.GetLastError(out nError, out sError);
        FSBOApp.StatusBar.SetText(e.Message, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
        OutLog("InitForm: " + nError.ToString() + " - " + sError + " - " + e.Message + " ** Trace: " + e.StackTrace);
      }
      return Result;
    }


    private void GenerarSalida(String DocEntry)
    {
      Recordset oRecordSetAux = FCmpny.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
      //TODO:
      // 

      if (!String.IsNullOrEmpty(DocEntry))
      {
        String ArticuloKey = "";

        oRecordSet.DoQuery(String.Format("SELECT DocNum, DocDate FROM ODLN Where DocEntry = {0}", DocEntry));
        String DocNum = ((Int32)oRecordSet.Fields.Item("DocNum").Value).ToString();
        DateTime FechaDoc = (DateTime)oRecordSet.Fields.Item("DocDate").Value;

        String StrSql = !GlobalSettings.RunningUnderSQLServer ?
            String.Format("", DocEntry) :
            String.Format("SELECT (SELECT COUNT(*) FROM DLN1 T0 Where T0.DocEntry = {0} AND (T0.U_VID_Bonif1 = 'Y' OR  T0.U_VID_Bonif2 = 'Y')) Lineas  FROM ODLN Where DocEntry = {0}", DocEntry);
        oRecordSet.DoQuery(StrSql);
        if ((Int32)oRecordSet.Fields.Item("Lineas").Value > 0)
        {
          OutLog(String.Format("Generando salida bonificacion"));
          SAPbobsCOM.Documents oDocument = (SAPbobsCOM.Documents)FSBOf.Cmpny.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryGenExit);
          oDocument.HandWritten = BoYesNoEnum.tNO;

          oDocument.TaxDate = FechaDoc;
          oDocument.DocDueDate = FechaDoc;
          oDocument.DocDate = FechaDoc;
          oDocument.Comments = "Bonificacion";
          oDocument.Reference1 = "";
          oDocument.Reference2 = DocNum;

          StrSql = !GlobalSettings.RunningUnderSQLServer ?
              String.Format("", DocEntry) :
              String.Format("SELECT ItemCode, U_VID_Bonif1, U_VID_Bonif2, U_VID_QtyBon1, U_VID_QtyBon2, U_VID_ArtBon2, U_VID_CtaBon, WhsCode, OcrCode, OcrCode2, OcrCode3, OcrCode4, OcrCode5, U_VID_LoteB1, U_VID_LoteB2 FROM DLN1 T0 Where DocEntry = {0} AND  (T0.U_VID_Bonif1 = 'Y' OR  T0.U_VID_Bonif2 = 'Y')", DocEntry);
          oRecordSet.DoQuery(StrSql);

          Int32 lin = 0;
          while (!(oRecordSet.EoF))
          {
            if (((String)oRecordSet.Fields.Item("U_VID_Bonif1").Value == "Y") || ((String)oRecordSet.Fields.Item("U_VID_Bonif2").Value == "Y"))
            {
              if ((String)oRecordSet.Fields.Item("U_VID_Bonif1").Value == "Y")
              {
                ArticuloKey = (String)oRecordSet.Fields.Item("ItemCode").Value;
                oRecordSetAux.DoQuery(String.Format("Select ManBtchNum FROM OITM Where ItemCode = '{0}'", ArticuloKey));
                if ((String)oRecordSetAux.Fields.Item("ManBtchNum").Value == "Y")
                {
                  if (ListLotesSelBonif1.ContainsKey(ArticuloKey))
                  {
                    List<TLotesInfo> ListArticulosBonif1;
                    ListArticulosBonif1 = ListLotesSelBonif1[ArticuloKey];
                    for (int i = 0; i <= ListArticulosBonif1.Count - 1; i++)
                    {
                      if (lin > 0) oDocument.Lines.Add();
                      oDocument.Lines.SetCurrentLine(oDocument.Lines.Count - 1);

                      oDocument.Lines.WarehouseCode = ListArticulosBonif1[i].Almacen;//(String)oRecordSet.Fields.Item("WhsCode").Value;
                      oDocument.Lines.Quantity = ListArticulosBonif1[i].Qty; //(Double)oRecordSet.Fields.Item("U_VID_QtyBon1").Value;
                      oDocument.Lines.ItemCode = (String)oRecordSet.Fields.Item("ItemCode").Value;
                      oDocument.Lines.AccountCode = (String)oRecordSet.Fields.Item("U_VID_CtaBon").Value;
                      oDocument.Lines.UserFields.Fields.Item("U_tipoOpT12").Value = "07";
                      oDocument.Lines.CostingCode = (String)oRecordSet.Fields.Item("OcrCode").Value;
                      oDocument.Lines.CostingCode2 = (String)oRecordSet.Fields.Item("OcrCode2").Value;
                      oDocument.Lines.CostingCode3 = (String)oRecordSet.Fields.Item("OcrCode3").Value;
                      oDocument.Lines.CostingCode4 = (String)oRecordSet.Fields.Item("OcrCode4").Value;
                      oDocument.Lines.CostingCode5 = (String)oRecordSet.Fields.Item("OcrCode5").Value;

                      oDocument.Lines.BatchNumbers.BatchNumber = ListArticulosBonif1[i].Lote;
                      oDocument.Lines.BatchNumbers.Quantity = ListArticulosBonif1[i].Qty;

                      lin = +1;
                    }
                  }
                }
              }

              if ((String)oRecordSet.Fields.Item("U_VID_Bonif2").Value == "Y")
              {
                ArticuloKey = (String)oRecordSet.Fields.Item("ItemCode").Value;
                oRecordSetAux.DoQuery(String.Format("Select ManBtchNum FROM OITM Where ItemCode = '{0}'", (String)oRecordSet.Fields.Item("U_VID_ArtBon2").Value));
                if ((String)oRecordSetAux.Fields.Item("ManBtchNum").Value == "Y")
                {
                  if (ListLotesSelBonif2.ContainsKey(ArticuloKey))
                  {
                    List<TLotesInfo> ListArticulosBonif2;
                    ListArticulosBonif2 = ListLotesSelBonif2[ArticuloKey];
                    for (int i = 0; i <= ListArticulosBonif2.Count - 1; i++)
                    {
                      if (lin > 0) oDocument.Lines.Add();
                      oDocument.Lines.SetCurrentLine(oDocument.Lines.Count - 1);

                      oDocument.Lines.WarehouseCode = ListArticulosBonif2[i].Almacen;//(String)oRecordSet.Fields.Item("WhsCode").Value;
                      oDocument.Lines.Quantity = ListArticulosBonif2[i].Qty;//(Double)oRecordSet.Fields.Item("U_VID_QtyBon2").Value;
                      oDocument.Lines.ItemCode = (String)oRecordSet.Fields.Item("U_VID_ArtBon2").Value;
                      oDocument.Lines.AccountCode = (String)oRecordSet.Fields.Item("U_VID_CtaBon").Value;
                      oDocument.Lines.UserFields.Fields.Item("U_tipoOpT12").Value = "07";
                      oDocument.Lines.CostingCode = (String)oRecordSet.Fields.Item("OcrCode").Value;
                      oDocument.Lines.CostingCode2 = (String)oRecordSet.Fields.Item("OcrCode2").Value;
                      oDocument.Lines.CostingCode3 = (String)oRecordSet.Fields.Item("OcrCode3").Value;
                      oDocument.Lines.CostingCode4 = (String)oRecordSet.Fields.Item("OcrCode4").Value;
                      oDocument.Lines.CostingCode5 = (String)oRecordSet.Fields.Item("OcrCode5").Value;

                      oDocument.Lines.BatchNumbers.BatchNumber = ListArticulosBonif2[i].Lote;
                      oDocument.Lines.BatchNumbers.Quantity = ListArticulosBonif2[i].Qty;

                      lin = +1;
                    }
                  }
                }
              }
            }
            oRecordSet.MoveNext();
          }

          //oDocument.SaveToFile(System.IO.Path.GetDirectoryName(TMultiFunctions.ParamStr(0)) + @"\Salida.XML");
          Int32 LRes = oDocument.Add();
          if (LRes == 0)
          {
            String DocEntrySalida = FCmpny.GetNewObjectKey();
            FSBOApp.StatusBar.SetText(String.Format("Salida de bonificacion generada, {0}", DocEntrySalida), BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Success);
            StrSql = !GlobalSettings.RunningUnderSQLServer ?
                String.Format("", DocEntry, DocEntrySalida) :
                String.Format("UPDATE ODLN SET U_VID_NumSal = {1} Where DocEntry = {0}", DocEntry, DocEntrySalida);
            oRecordSet.DoQuery(StrSql);
          }
          else
          {
            Int32 nErr;
            String sErr;
            FCmpny.GetLastError(out nErr, out sErr);
            OutLog(String.Format("Error en generacion de salida bonificacion, {0} : {1}", nErr, sErr));
            FSBOApp.StatusBar.SetText(String.Format("Error en generacion de salida bonificacion, {0} : {1}", nErr, sErr), BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
          }
        }
      }
    }

    public new void FormDataEvent(ref BusinessObjectInfo BusinessObjectInfo, ref bool BubbleEvent)
    {
      base.FormDataEvent(ref BusinessObjectInfo, ref BubbleEvent);
      try
      {
        if ((!BusinessObjectInfo.BeforeAction) && (BusinessObjectInfo.EventType == BoEventTypes.et_FORM_DATA_ADD) && (BusinessObjectInfo.ActionSuccess))
        {
          String DocEntry = FSBOf.GetDocEntryBusinessObjectInfo(BusinessObjectInfo.ObjectKey);
          try
          {
            //guardar datos lotes
            TSBOGeneralService oGen = new TSBOGeneralService();
            try
            {
              oGen.SBO_f = FSBOf;
              String SQLStr = !GlobalSettings.RunningUnderSQLServer ? "SELECT (IFNULL(MAX(CAST(\"Code\" AS INT)), 0) + 1) \"Resultado\" FROM \"@VID_BEBLOT\"" : "SELECT ISNULL(MAX(CAST(Code AS INT)), 0) + 1 Resultado FROM [@VID_BEBLOT]";
              oRecordSet.DoQuery(SQLStr);
              Int32 CodeInt = ((int)oRecordSet.Fields.Item("Resultado").Value);
              String TipoDoc = "E"; //Entrega
              String[] fieldsId = new String[] { "Code", "Name", "U_ItemCode", "U_Lote", "U_Cantidad", "U_DocEntry", "U_TipoDoc", "U_Almacen", "U_TipoBonif" };

              foreach (KeyValuePair<String, List<TLotesInfo>> entry in ListLotesSelBonif1)
              {
                foreach (TLotesInfo item in entry.Value)
                {
                  String CodeStr = CodeInt.ToString();
                  String TipoBonif = "1";
                  object[] values = new object[] { CodeStr, CodeStr, entry.Key, item.Lote, item.Qty, DocEntry, TipoDoc, item.Almacen, TipoBonif};
                  oGen.InsertUDOHeaderData("VID_BEBLOT", fieldsId, values);
                  CodeInt += 1;
                }
              }
              
              foreach (KeyValuePair<String, List<TLotesInfo>> entry in ListLotesSelBonif2)
              {
                foreach (TLotesInfo item in entry.Value)
                {
                  String CodeStr = CodeInt.ToString();
                  String TipoBonif = "2";
                  object[] values = new object[] { CodeStr, CodeStr, entry.Key, item.Lote, item.Qty, DocEntry, TipoDoc, item.Almacen, TipoBonif };
                  oGen.InsertUDOHeaderData("VID_BEBLOT", fieldsId, values);
                  CodeInt += 1;
                }
              }

            }
            finally
            {
              FSBOf._ReleaseCOMObject(oGen);
            }
            //Realizar salida
            GenerarSalida(DocEntry);
          }
          finally
          {
            ListLotesSelBonif1.Clear();
            ListLotesSelBonif2.Clear();
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

    public new void FormEvent(string FormUID, ref ItemEvent pVal, ref bool BubbleEvent)
    {
      base.FormEvent(FormUID, ref pVal, ref BubbleEvent);
      try
      {
        //if ((pVal.EventType == SAPbouiCOM.BoEventTypes.et_GOT_FOCUS) && !pVal.BeforeAction && (oForm.Mode == BoFormMode.fm_ADD_MODE))
        //{
        //  if (((pVal.ColUID == "U_VID_QtyBon1") || (pVal.ColUID == "U_VID_QtyBon2")) && (pVal.Row > 0))
        //  {
        //    if (LastValues.ContainsKey(pVal.ColUID))
        //      LastValues.Remove(pVal.ItemUID);
        //    LastValues.Add(pVal.ItemUID, ((SAPbouiCOM.EditText)oMatrix.Columns.Item(pVal.ColUID).Cells.Item(pVal.Row).Specific).Value);
        //  }
        //}

        //if ((pVal.EventType == SAPbouiCOM.BoEventTypes.et_LOST_FOCUS) && !pVal.BeforeAction && (oForm.Mode == BoFormMode.fm_ADD_MODE))
        //{
        //  if (((pVal.ColUID == "U_VID_QtyBon1") || (pVal.ColUID == "U_VID_QtyBon2")) && (pVal.Row > 0))
        //  {


        //  }
        //}

        if ((pVal.EventType == SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED) && pVal.BeforeAction && (oForm.Mode == BoFormMode.fm_ADD_MODE))
        {
          switch (pVal.ItemUID)
          {
            case "1":
              BubbleEvent = false;
              for (int i = 1; i <= oMatrix.RowCount; i++)
              {
                String Articulo = ((SAPbouiCOM.EditText)oMatrix.Columns.Item("1").Cells.Item(i).Specific).Value;
                String ArticuloKey = ((SAPbouiCOM.EditText)oMatrix.Columns.Item("1").Cells.Item(i).Specific).Value;
                if (!String.IsNullOrEmpty(Articulo) && Bonif1Checked(i))
                {
                  oRecordSet.DoQuery(String.Format("Select ManBtchNum FROM OITM Where ItemCode = '{0}'", Articulo));
                  if (((String)oRecordSet.Fields.Item("ManBtchNum").Value == "Y") && !ListLotesSelBonif1.ContainsKey(ArticuloKey))
                    throw new Exception(String.Format("Articulo {0} en fila {1} no tiene asignado los lotes a utilizar en la bonificacion 1", Articulo, i));
                }

                Articulo = ((EditText)oMatrix.Columns.Item("U_VID_ArtBon2").Cells.Item(i).Specific).Value;
                if (!String.IsNullOrEmpty(Articulo) && Bonif2Checked(i))
                {
                  oRecordSet.DoQuery(String.Format("Select ManBtchNum FROM OITM Where ItemCode = '{0}'", Articulo));
                  if (((String)oRecordSet.Fields.Item("ManBtchNum").Value == "Y") && !ListLotesSelBonif2.ContainsKey(ArticuloKey))
                    throw new Exception(String.Format("Articulo {0} en fila {1} no tiene asignado los lotes a utilizar en la bonificacion 2", Articulo, i));
                }
              }
              BubbleEvent = true;
              break;
          }
        }
        else
          if ((pVal.EventType == SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED) && !pVal.BeforeAction && (oForm.Mode == BoFormMode.fm_ADD_MODE))
          {
            Boolean Found = false;
            switch (pVal.ItemUID)
            {
              case "btn_AsigL1":
                for (int i = 0; i <= ooForms.Count - 1; i++)
                  if (ooForms[i] is TAsignaLotesBonif)
                  {
                    Found = true;
                    FSBOApp.StatusBar.SetText("La ventana de asignacion de lotes ya esta abierta", BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Warning);
                    break;
                  }

                if (!Found)
                {
                  Int32 Fila = FSBOf.GetSelectedRow(oMatrix);
                  if (Fila >= 1)
                  {
                    if (!Bonif1Checked(Fila))
                    {
                      FSBOApp.StatusBar.SetText("Campo de bonificacion 1 no activo", BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
                    }
                    else
                      AsignarLotesBonif(Fila, "U_VID_QtyBon1", "1", ListLotesSelBonif1);
                  }
                  else
                    FSBOApp.StatusBar.SetText("Debe seleccionar una fila para ejecutar La asignacion de Lotes", BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
                }
                break;

              case "btn_AsigL2":
                for (int i = 0; i <= ooForms.Count - 1; i++)
                  if (ooForms[i] is TAsignaLotesBonif)
                  {
                    Found = true;
                    FSBOApp.StatusBar.SetText("La ventana de asignacion de lotes ya esta abierta", BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Warning);
                    break;
                  }

                if (!Found)
                {
                  Int32 Fila = FSBOf.GetSelectedRow(oMatrix);
                  if (Fila >= 1)
                  {
                    if (!Bonif2Checked(Fila))
                    {
                      FSBOApp.StatusBar.SetText("Campo de bonificacion 2 no activo", BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
                    }
                    else
                      AsignarLotesBonif(Fila, "U_VID_QtyBon2", "U_VID_ArtBon2", ListLotesSelBonif2);
                  }
                  else
                    FSBOApp.StatusBar.SetText("Debe seleccionar una fila para ejecutar La asignacion de Lotes", BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
                }
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

    private void AsignarLotesBonif(Int32 Fila, String ColumnIDQty, String ColumnArticulo, Dictionary<String, List<TLotesInfo>> ListaLotesSel)
    {
      try
      {
        String Articulo = ((EditText)oMatrix.Columns.Item(ColumnArticulo).Cells.Item(Fila).Specific).Value;
        String ArticuloKey = ((EditText)oMatrix.Columns.Item("1").Cells.Item(Fila).Specific).Value;
        String Almacen = ((EditText)oMatrix.Columns.Item("24").Cells.Item(Fila).Specific).Value;
        Double Quantity = FSBOf.StrToDouble(((EditText)oMatrix.Columns.Item(ColumnIDQty).Cells.Item(Fila).Specific).Value);

        if (String.IsNullOrEmpty(ArticuloKey))
          throw new Exception(String.Format("Articulo vacio en fila {0}", Fila));

        List<TLotesInfo> ListaLocal;
        if (ListaLotesSel.ContainsKey(ArticuloKey))
          ListaLocal = ListaLotesSel[ArticuloKey];
        else
        {
          ListaLocal = new List<TLotesInfo>();
          ListaLotesSel.Add(ArticuloKey, ListaLocal);
        }

        if (String.IsNullOrEmpty(Articulo))
          throw new Exception(String.Format("Articulo de bonificacion vacio en fila {0}", Fila));

        if (String.IsNullOrEmpty(Almacen))
          throw new Exception(String.Format("Almacen vacio en fila {0}", Fila));

        //validar art. soporta lotes
        oRecordSet.DoQuery(String.Format("SELECT ManBtchNum, DfltWH FROM OITM WHERE ItemCode = '{0}'", Articulo));
        if (ColumnArticulo != "1")
          Almacen = (String)oRecordSet.Fields.Item("DfltWH").Value;

        if ((String)oRecordSet.Fields.Item("ManBtchNum").Value != "Y")
          throw new Exception(String.Format("Articulo {0} en fila {0} no soporta manejo de lotes", Articulo, Fila));

        if (Quantity <= 0.0)
          throw new Exception(String.Format("La cantidad a bonificar debe ser mayor a 0 - fila {0}, Columna {1}", Fila, ColumnIDQty));

        IvkFormInterface oFormVk = (IvkFormInterface)(new TAsignaLotesBonif());
        ((TAsignaLotesBonif)oFormVk).Articulo = Articulo;
        ((TAsignaLotesBonif)oFormVk).Almacen = Almacen;
        ((TAsignaLotesBonif)oFormVk).MaxQty = Quantity;
        ((TAsignaLotesBonif)oFormVk).ListLotesSel = ListaLocal;
        String oUid = FSBOf.generateFormId(FGlobalSettings.SBOSpaceName, FGlobalSettings);
        oFormVk.InitForm(oUid, "forms\\", ref FSBOApp, ref FCmpny, ref FSBOf, ref FGlobalSettings);
        FoForms.Add(oFormVk);
      }
      catch (Exception e)
      {
        OutLog(e.Message + " ** Trace: " + e.StackTrace);
        FSBOApp.StatusBar.SetText(e.Message, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
      }
    }
  }
}
