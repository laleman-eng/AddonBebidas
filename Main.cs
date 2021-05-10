using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using SAPbobsCOM;
using System.Windows.Forms;
using VisualD.Main;
using VisualD.MultiFunctions;


namespace Bebidas
{

  public partial class FrmMain : Form
  {
    private TMainClassExt MainClass = null;
    public FrmMain()
    {
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
      Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
      InitializeComponent();
      MainClass = new TMainClassExt();
      MainClass.MainObj.Add(new LMainObjBase());
      MainClass.Init();
    }

    public void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if ((MainClass != null) && (MainClass.oLog != null))
      {
        MainClass.oLog.OutLog(String.Format("CurrentDomain_UnhandledException {0} Trace {1}", ((Exception)e.ExceptionObject).Message, ((Exception)e.ExceptionObject).StackTrace));
      }
      MessageBox.Show(String.Format("CurrentDomain_UnhandledException {0} Trace {1}", ((Exception)e.ExceptionObject).Message, ((Exception)e.ExceptionObject).StackTrace), "Unhandled UI Exception");

    }

    public void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
      if ((MainClass != null) && (MainClass.oLog != null))
      {
        MainClass.oLog.OutLog(String.Format("Application_ThreadException {0} Trace {1}", e.Exception.Message, e.Exception.StackTrace));
      }

      MessageBox.Show(String.Format("Application_ThreadException {0} Trace {1}", e.Exception.Message, e.Exception.StackTrace), "Unhandled Thread Exception");
    }

    private void Form1_Load(object sender, EventArgs e)
    {
      ShowInTaskbar = false;
      Hide();
    }
  }

  public class TMainClassExt : TMainClass
  {
    public TMainClassExt()
      : base()
    {
      GlobalSettings.SBOSpaceName = "VID_Bebidas";
    }

    public override void SetFiltros()
    {
      base.SetFiltros();
      SAPbouiCOM.EventFilters oFilters = SBOApplication.GetFilter();
      SAPbouiCOM.EventFilter oFilter;

      //oFilter = SBOFunctions.GetFilterByEventType(oFilters, SAPbouiCOM.BoEventTypes.et_FORM_RESIZE);
      //oFilter.AddEx("VID_SYNPRFL");
      //oFilter.AddEx("VID_VIAJES");

      oFilter = SBOFunctions.GetFilterByEventType(oFilters, SAPbouiCOM.BoEventTypes.et_ITEM_PRESSED);
      oFilter.AddEx("140");//Entrega
      oFilter.AddEx("VID_AsigLotes");

      oFilter = SBOFunctions.GetFilterByEventType(oFilters, SAPbouiCOM.BoEventTypes.et_VALIDATE);
      oFilter.AddEx("VID_AsigLotes");

      //oFilter = SBOFunctions.GetFilterByEventType(oFilters, SAPbouiCOM.BoEventTypes.et_CHOOSE_FROM_LIST);
      //oFilter.AddEx("VID_SYNPRFL");

      //oFilter = SBOFunctions.GetFilterByEventType(oFilters, SAPbouiCOM.BoEventTypes.et_FORM_CLOSE);
      //oFilter.AddEx("VID_VIAJES");

      //oFilter = SBOFunctions.GetFilterByEventType(oFilters, SAPbouiCOM.BoEventTypes.et_GOT_FOCUS);
      //oFilter.AddEx("140");

      //oFilter = SBOFunctions.GetFilterByEventType(oFilters, SAPbouiCOM.BoEventTypes.et_LOST_FOCUS);
      //oFilter.AddEx("140");

      oFilter = SBOFunctions.GetFilterByEventType(oFilters, SAPbouiCOM.BoEventTypes.et_FORM_DATA_ADD);
      oFilter.AddEx("140");//Entrega
      oFilter.AddEx("180");//Devolucion
      oFilter.AddEx("179");//NC

      SBOApplication.SetFilter(oFilters);
    }

    public override void initApp()
    {
      base.initApp();
      InitOK = false;
      GlobalSettings.SBO_f = SBOFunctions;

      SAPbobsCOM.Recordset oRecordSet = SBOCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset) as Recordset;
      oRecordSet.DoQuery(string.Format("SELECT SUPERUSER FROM OUSR WHERE USERID = {0}", SBOCompany.UserSignature));
      bool superuser = (oRecordSet.Fields.Item("SUPERUSER").Value as string) == "Y";
      SBOFunctions._ReleaseCOMObject(oRecordSet);


      if (!superuser ? (SBOCompany.UserName == "manager") : true)
      {
        string XlsFile = System.IO.Path.GetDirectoryName(TMultiFunctions.ParamStr(0)) + @"\Docs\" + "EDBEBIDAS.xls";
        if (!SBOFunctions.ValidEstructSHA1(XlsFile))
        {
          oLog.OutLog("InitApp: Estructura de datos (1)");
          SBOApplication.StatusBar.SetText("Inicializando AddOn Bebidas", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
          if (!SBOMetaData.SyncTablasUdos("1.1", XlsFile))
          {
            SBOFunctions.DeleteSHA1FromTable("EDBEBIDAS.xls");
            oLog.OutLog("InitApp: sincronizaci\x00f3n de Estructura de datos fallo");

            SBOApplication.MessageBox("Estructura de datos con problemas, consulte a soporte...", 1, "Ok", "", "");
            Halt(0);
          }
        }

        XlsFile = System.IO.Path.GetDirectoryName(TMultiFunctions.ParamStr(0)) + @"\Docs\" + "EDBEBIDAS2.xls";
        if (!SBOFunctions.ValidEstructSHA1(XlsFile))
        {
          oLog.OutLog("InitApp: Estructura de datos (1)");
          SBOApplication.StatusBar.SetText("Inicializando AddOn Bebidas", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Warning);
          if (!SBOMetaData.SyncTablasUdos("1.1", XlsFile))
          {
            SBOFunctions.DeleteSHA1FromTable("EDBEBIDAS2.xls");
            oLog.OutLog("InitApp: sincronizaci\x00f3n de Estructura de datos fallo");

            SBOApplication.MessageBox("Estructura de datos con problemas, consulte a soporte...", 1, "Ok", "", "");
            Halt(0);
          }
        }

      }

      MainObj[0].GlobalSettings = GlobalSettings;
      MainObj[0].SBOApplication = SBOApplication;
      MainObj[0].SBOCompany = SBOCompany;
      MainObj[0].oLog = oLog;
      MainObj[0].SBOFunctions = SBOFunctions;
      SetFiltros();
      MainObj[0].AddMenus();

      InitOK = true;
      oLog.OutLog("C# - Shine your crazy diamond!");
      SBOApplication.StatusBar.SetText("Aplicación Inicializada.", SAPbouiCOM.BoMessageTime.bmt_Short, SAPbouiCOM.BoStatusBarMessageType.smt_Success);
    }

  }


}
