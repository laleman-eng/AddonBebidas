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
using System.IO;

namespace Bebidas
{
  class TNotaCreditoSBO : TvkBaseForm, IvkFormInterface
  {
    private int nError;
    private Form oForm;
    private Recordset oRecordSet;
    private String sError;
    //private SAPbouiCOM.DBDataSource oDBDS;

    public new bool InitForm(string uid, string xmlPath, ref Application application, ref SAPbobsCOM.Company company, ref CSBOFunctions SBOFunctions, ref TGlobalVid _GlobalSettings)
    {
      bool Result = base.InitForm(uid, xmlPath, ref application, ref company, ref SBOFunctions, ref _GlobalSettings);
      try
      {
        oForm = FSBOApp.Forms.Item(uid);
        oRecordSet = FCmpny.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
        //oDBDS = oForm.DataSources.DBDataSources.Item("ODLN");

      }
      catch (Exception e)
      {
        FCmpny.GetLastError(out nError, out sError);
        FSBOApp.StatusBar.SetText(e.Message, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
        OutLog("InitForm: " + nError.ToString() + " - " + sError + " - " + e.Message + " ** Trace: " + e.StackTrace);
      }
      return Result;
    }

    private void RealizarEntrada(String DocEntry)
    {
      //TODO :
      // En NC ,  se debe hacer una entrada (e debe usar la info de la ventana (campos)). pero solo cuando la nc este basada en una factura, que a su vez esta basada en una entrega que tiene.
      Recordset oRecordSetAux = FCmpny.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;

      if (!String.IsNullOrEmpty(DocEntry))
      {

        oRecordSet.DoQuery(String.Format("SELECT DocNum, DocDate FROM ORIN Where DocEntry = {0}", DocEntry));
        String DocNum = ((Int32)oRecordSet.Fields.Item("DocNum").Value).ToString();
        DateTime FechaDoc = (DateTime)oRecordSet.Fields.Item("DocDate").Value;
        //Se obtiene DocEntry de Factura original
        String StrSql = !GlobalSettings.RunningUnderSQLServer ?
            String.Format("", DocEntry) :
            String.Format("SELECT TOP 1 BaseEntry FROM RIN1 T0 Where T0.DocEntry = {0} AND T0.BaseType =  '13' AND (T0.U_VID_Bonif1 = 'Y' OR  T0.U_VID_Bonif2 = 'Y')", DocEntry);
        oRecordSet.DoQuery(StrSql);
        Int32 DocEntryFactura = (Int32)oRecordSet.Fields.Item("BaseEntry").Value;

        //Se obtiene DocEntry de Entrega original
        StrSql = !GlobalSettings.RunningUnderSQLServer ?
            String.Format("", DocEntry) :
            String.Format("SELECT TOP 1 BaseEntry FROM INV1 T0 Where T0.DocEntry = {0} AND T0.BaseType =  '15' AND (T0.U_VID_Bonif1 = 'Y' OR  T0.U_VID_Bonif2 = 'Y')", DocEntryFactura);
        oRecordSet.DoQuery(StrSql);
        Int32 BaseEntry = (Int32)oRecordSet.Fields.Item("BaseEntry").Value;

        //Se obtiene num de salida de Entrega original
        StrSql = !GlobalSettings.RunningUnderSQLServer ?
            String.Format("", DocEntry) :
            String.Format("SELECT U_VID_NumSal FROM ODLN T0 Where T0.DocEntry = {0}", BaseEntry);
        oRecordSet.DoQuery(StrSql);
        Int32 NumSalida = (Int32)oRecordSet.Fields.Item("U_VID_NumSal").Value;



        StrSql = !GlobalSettings.RunningUnderSQLServer ?
            String.Format("", DocEntry) :
            String.Format("SELECT (SELECT COUNT(*) FROM RIN1 T0 Where T0.DocEntry = {0} AND T0.BaseType =  '13' AND (T0.U_VID_Bonif1 = 'Y' OR  T0.U_VID_Bonif2 = 'Y')) Lineas FROM ORIN Where DocEntry = {0}", DocEntry);
        oRecordSet.DoQuery(StrSql);
        if ((Int32)oRecordSet.Fields.Item("Lineas").Value > 0)
        {
          OutLog(String.Format("Generando entrada bonificacion"));
          SAPbobsCOM.Documents oDocument = (SAPbobsCOM.Documents)FSBOf.Cmpny.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oInventoryGenEntry);
          oDocument.HandWritten = BoYesNoEnum.tNO;

          oDocument.TaxDate = FechaDoc;
          oDocument.DocDueDate = FechaDoc;
          oDocument.DocDate = FechaDoc;
          oDocument.Comments = "Devolucion Bonificacion";
          oDocument.Reference1 = "";
          oDocument.Reference2 = DocNum;

          StrSql = !GlobalSettings.RunningUnderSQLServer ?
              String.Format("", DocEntry) :
              String.Format("SELECT ItemCode, U_VID_Bonif1, U_VID_Bonif2, U_VID_QtyBon1, U_VID_QtyBon2, U_VID_ArtBon2, U_VID_CtaBon, WhsCode, OcrCode, OcrCode2, OcrCode3, OcrCode4, OcrCode5, U_VID_LoteB1, U_VID_LoteB2 FROM RIN1 T0 Where DocEntry = {0} AND  (T0.U_VID_Bonif1 = 'Y' OR  T0.U_VID_Bonif2 = 'Y')", DocEntry);
          oRecordSet.DoQuery(StrSql);

          Int32 lin = 0;
          while (!(oRecordSet.EoF))
          {
            if (((String)oRecordSet.Fields.Item("U_VID_Bonif1").Value == "Y") || ((String)oRecordSet.Fields.Item("U_VID_Bonif2").Value == "Y"))
            {
              if ((String)oRecordSet.Fields.Item("U_VID_Bonif1").Value == "Y")
              {
                if (lin > 0) oDocument.Lines.Add();
                oDocument.Lines.SetCurrentLine(oDocument.Lines.Count - 1);

                oDocument.Lines.WarehouseCode = (String)oRecordSet.Fields.Item("WhsCode").Value;
                oDocument.Lines.Quantity = (Double)oRecordSet.Fields.Item("U_VID_QtyBon1").Value;
                oDocument.Lines.ItemCode = (String)oRecordSet.Fields.Item("ItemCode").Value;
                oDocument.Lines.AccountCode = (String)oRecordSet.Fields.Item("U_VID_CtaBon").Value;

                oRecordSetAux.DoQuery(String.Format("Select StockPrice FROM IGE1 Where DocEntry = {0} AND ItemCode = '{1}'", NumSalida, (String)oRecordSet.Fields.Item("ItemCode").Value));
                oDocument.Lines.UnitPrice = (Double)oRecordSetAux.Fields.Item("StockPrice").Value;

                oDocument.Lines.UserFields.Fields.Item("U_tipoOpT12").Value = "07";

                oDocument.Lines.CostingCode = (String)oRecordSet.Fields.Item("OcrCode").Value;
                oDocument.Lines.CostingCode2 = (String)oRecordSet.Fields.Item("OcrCode2").Value;
                oDocument.Lines.CostingCode3 = (String)oRecordSet.Fields.Item("OcrCode3").Value;
                oDocument.Lines.CostingCode4 = (String)oRecordSet.Fields.Item("OcrCode4").Value;
                oDocument.Lines.CostingCode5 = (String)oRecordSet.Fields.Item("OcrCode5").Value;

                oRecordSetAux.DoQuery(String.Format("Select ManBtchNum FROM OITM Where ItemCode = '{0}'", (String)oRecordSet.Fields.Item("ItemCode").Value));
                if ((String)oRecordSetAux.Fields.Item("ManBtchNum").Value == "Y")
                {
                  //oDocument.Lines.BatchNumbers.BatchNumber = (String)oRecordSet.Fields.Item("U_VID_LoteB1").Value;                                           
                  //oDocument.Lines.BatchNumbers.Quantity = (Double)oRecordSet.Fields.Item("U_VID_QtyBon1").Value;
                  oRecordSetAux.DoQuery(String.Format(@"SELECT T2.DistNumber, ABS(T1.Quantity) Quantity FROM OITL T0 
                      INNER JOIN ITL1 T1 ON T1.LogEntry = T0.LogEntry
                      INNER JOIN OBTN T2 ON T2.SysNumber = T1.SysNumber AND T2.ItemCode = T1.ItemCode 
                      WHERE T0.ApplyEntry = {1} AND T0.ApplyType = 60 AND T0.ItemCode = '{0}'", (String)oRecordSet.Fields.Item("ItemCode").Value, NumSalida));
                  Int32 linelote = 0;
                  while (!oRecordSetAux.EoF)
                  {
                    if (linelote > 0)
                      oDocument.Lines.BatchNumbers.Add();
                    oDocument.Lines.BatchNumbers.BatchNumber = (String)oRecordSetAux.Fields.Item("DistNumber").Value;
                    oDocument.Lines.BatchNumbers.Quantity = (Double)oRecordSetAux.Fields.Item("Quantity").Value;
                    linelote++;
                    oRecordSetAux.MoveNext();
                  }
                }
                lin = +1;
              }

              if ((String)oRecordSet.Fields.Item("U_VID_Bonif2").Value == "Y")
              {
                if (lin > 0) oDocument.Lines.Add();
                oDocument.Lines.SetCurrentLine(oDocument.Lines.Count - 1);

                oDocument.Lines.WarehouseCode = (String)oRecordSet.Fields.Item("WhsCode").Value;
                oDocument.Lines.Quantity = (Double)oRecordSet.Fields.Item("U_VID_QtyBon2").Value;
                oDocument.Lines.ItemCode = (String)oRecordSet.Fields.Item("U_VID_ArtBon2").Value;
                oDocument.Lines.AccountCode = (String)oRecordSet.Fields.Item("U_VID_CtaBon").Value;

                oRecordSetAux.DoQuery(String.Format("Select StockPrice FROM IGE1 Where DocEntry = {0} AND ItemCode = '{1}'", NumSalida, (String)oRecordSet.Fields.Item("U_VID_ArtBon2").Value));
                oDocument.Lines.UnitPrice = (Double)oRecordSetAux.Fields.Item("StockPrice").Value;

                oDocument.Lines.UserFields.Fields.Item("U_tipoOpT12").Value = "07";

                oDocument.Lines.CostingCode = (String)oRecordSet.Fields.Item("OcrCode").Value;
                oDocument.Lines.CostingCode2 = (String)oRecordSet.Fields.Item("OcrCode2").Value;
                oDocument.Lines.CostingCode3 = (String)oRecordSet.Fields.Item("OcrCode3").Value;
                oDocument.Lines.CostingCode4 = (String)oRecordSet.Fields.Item("OcrCode4").Value;
                oDocument.Lines.CostingCode5 = (String)oRecordSet.Fields.Item("OcrCode5").Value;

                oRecordSetAux.DoQuery(String.Format("Select ManBtchNum FROM OITM Where ItemCode = '{0}'", (String)oRecordSet.Fields.Item("U_VID_ArtBon2").Value));
                if ((String)oRecordSetAux.Fields.Item("ManBtchNum").Value == "Y")
                {
                  //oDocument.Lines.BatchNumbers.BatchNumber = (String)oRecordSet.Fields.Item("U_VID_LoteB2").Value;
                  //oDocument.Lines.BatchNumbers.Quantity = (Double)oRecordSet.Fields.Item("U_VID_QtyBon2").Value;
                  oRecordSetAux.DoQuery(String.Format(@"SELECT T2.DistNumber, ABS(T1.Quantity) Quantity FROM OITL T0 
                                            INNER JOIN ITL1 T1 ON T1.LogEntry = T0.LogEntry
                                            INNER JOIN OBTN T2 ON T2.SysNumber = T1.SysNumber AND T2.ItemCode = T1.ItemCode 
                                            WHERE T0.ApplyEntry = {1} AND T0.ApplyType = 60 AND T0.ItemCode = '{0}'", (String)oRecordSet.Fields.Item("U_VID_ArtBon2").Value, NumSalida));
                  Int32 linelote = 0;
                  while (!oRecordSetAux.EoF)
                  {
                    if (linelote > 0)
                      oDocument.Lines.BatchNumbers.Add();
                    oDocument.Lines.BatchNumbers.BatchNumber = (String)oRecordSetAux.Fields.Item("DistNumber").Value;
                    oDocument.Lines.BatchNumbers.Quantity = (Double)oRecordSetAux.Fields.Item("Quantity").Value;
                    linelote++;
                    oRecordSetAux.MoveNext();
                  }

                  lin = +1;
                }

              }
            }
            oRecordSet.MoveNext();
          }

          Int32 LRes = oDocument.Add();
          if (LRes == 0)
          {
            String DocEntrySalida = FCmpny.GetNewObjectKey();
            FSBOApp.StatusBar.SetText(String.Format("Entrada de bonificacion generada, {0}", DocEntrySalida), BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Success);
            StrSql = !GlobalSettings.RunningUnderSQLServer ?
                String.Format("", DocEntry, DocEntrySalida) :
                String.Format("UPDATE ORIN SET U_VID_NumDev = {1} Where DocEntry = {0}", DocEntry, DocEntrySalida);
            oRecordSet.DoQuery(StrSql);
          }
          else
          {
            Int32 nErr;
            String sErr;
            FCmpny.GetLastError(out nErr, out sErr);
            OutLog(String.Format("Error en generacion de entrada bonificacion, {0} : {1}", nErr, sErr));
            FSBOApp.StatusBar.SetText(String.Format("Error en generacion de entrada bonificacion, {0} : {1}", nErr, sErr), BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
            oDocument.SaveToFile(Path.Combine(Path.GetTempPath(), string.Format("Entrada_NC_{0}.xml", DocEntry)));
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
          RealizarEntrada(DocEntry);
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
        if ((pVal.EventType == SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED) && pVal.BeforeAction && (oForm.Mode == BoFormMode.fm_ADD_MODE))
        {
          switch (pVal.ItemUID)
          {
            case "1":
              //Foo();
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
  }
}
