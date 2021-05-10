using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using VisualD.MainObjBase;
using VisualD.vkBaseForm;
using VisualD.vkFormInterface;
using VisualD.MultiFunctions;
using SAPbouiCOM;

namespace Bebidas
{
  public class LMainObjBase : TMainObjBase
  {
    public void CreateMenus(Boolean AddMnu)
    {
    }

    public override void AddMenus()
    {
      CreateMenus(true);
    }

    public override void RemoveMenus()
    {
      CreateMenus(false);
    }

    public override IvkFormInterface ItemEventExt(IvkFormInterface oIvkForm, List<object> oForms, string LstFrmUID, string FormUID, ref ItemEvent pVal, ref Boolean BubbleEvent)
    {
      try
      {
        SAPbouiCOM.Form oForm;
        SAPbouiCOM.Form oFormParent;
        IvkFormInterface result = null;

        result = base.ItemEventExt(oIvkForm, oForms, LstFrmUID, FormUID, ref pVal, ref BubbleEvent);

        if (result != null)
        {
          return result;
        }
        else
        {
          if (oIvkForm != null)
          {
            return oIvkForm;
          }
        }

        // CFL Extendido (Enmascara el CFL estandar)
        if ((pVal.BeforeAction) && (pVal.EventType == BoEventTypes.et_FORM_LOAD) && (!string.IsNullOrEmpty(LstFrmUID)))
        {
          try
          {
            oForm = SBOApplication.Forms.Item(LstFrmUID);
          }
          catch
          {
            oForm = null;
          }
        }


        if ((!pVal.BeforeAction) && (pVal.EventType == BoEventTypes.et_FORM_LOAD) && (oIvkForm == null))
        {
          if (pVal.FormTypeEx == "140")
          {
            result = (IvkFormInterface)new TEntregaSBO();
          }

          if (pVal.FormTypeEx == "180")
          {
            result = (IvkFormInterface)new TDevolucionSBO();
          }


          if (pVal.FormTypeEx == "179")
          {
            result = (IvkFormInterface)new TNotaCreditoSBO();
          }
        }


        if ((!pVal.BeforeAction) && (pVal.FormTypeEx == "0"))
        {
          if ((oIvkForm == null) && (GlobalSettings.UsrFldsFormActive) && (GlobalSettings.UsrFldsFormUid != "") && (pVal.EventType == BoEventTypes.et_FORM_LOAD))
          {
            oForm = SBOApplication.Forms.Item(pVal.FormUID);
            oFormParent = SBOApplication.Forms.Item(GlobalSettings.UsrFldsFormUid);
            try
            {
              //SBO_App.StatusBar.SetText(oFormParent.Title,BoMessageTime.bmt_Short,BoStatusBarMessageType.smt_Warning);
              SBOFunctions.FillListUserFieldForm(GlobalSettings.ListFormsUserField, oFormParent, oForm);
            }
            finally
            {
              GlobalSettings.UsrFldsFormUid = "";
              GlobalSettings.UsrFldsFormActive = false;
            }
          }
          else
          {
            if ((pVal.EventType == BoEventTypes.et_FORM_ACTIVATE) || (pVal.EventType == BoEventTypes.et_COMBO_SELECT) || (pVal.EventType == BoEventTypes.et_FORM_RESIZE))
            {
              oForm = SBOApplication.Forms.Item(pVal.FormUID);
              SBOFunctions.DisableListUserFieldsForm(GlobalSettings.ListFormsUserField, oForm);
            }
          }

        }

        if (result != null)
        {
          SAPbouiCOM.Application App = SBOApplication;
          SAPbobsCOM.Company Cmpny = SBOCompany;
          VisualD.SBOFunctions.CSBOFunctions SboF = SBOFunctions;
          VisualD.GlobalVid.TGlobalVid Glob = GlobalSettings;
          if (result.InitForm(pVal.FormUID, @"forms\", ref App, ref Cmpny, ref SboF, ref Glob))
          {
            oForms.Add(result);
          }
          else
          {
            SBOApplication.Forms.Item(result.getFormId()).Close();
            result = null;
          }
        }

        return result;
      }
      catch (Exception e)
      {
        oLog.OutLog("ItemEventExt: " + e.Message + " ** Trace: " + e.StackTrace);
        SBOApplication.StatusBar.SetText(e.Message + " ** Trace: " + e.StackTrace, BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Error);
        return null;
      }


    }




    public override void MenuEventExt(List<object> oForms, ref MenuEvent pVal, ref bool BubbleEvent)
    {
      try
      {
        IvkFormInterface oForm = null;
        if (pVal.BeforeAction)
        {
          return;
        }


        //if (pVal.MenuUID == "VID_SYN_FL1")
        //{
        //     oForm = new TProvisionFletes() as IvkFormInterface;
        //}


        if (oForm != null)
        {
          Application App = SBOApplication;
          SAPbobsCOM.Company Cmpny = SBOCompany;
          VisualD.SBOFunctions.CSBOFunctions SboF = SBOFunctions;
          VisualD.GlobalVid.TGlobalVid Glob = GlobalSettings;
          if (oForm.InitForm(SBOFunctions.generateFormId(GlobalSettings.SBOSpaceName, GlobalSettings), @"forms\", ref App, ref Cmpny, ref SboF, ref Glob))
          {
            oForms.Add(oForm);
          }
          else
          {
            this.SBOApplication.Forms.Item(oForm.getFormId()).Close();
            oForm = null;
          }
        }


      }
      catch (Exception e)
      {
        this.oLog.OutLog("MenuEventExt: " + e.Message + " ** Trace: " + e.StackTrace);
        this.SBOApplication.MessageBox(e.Message + " ** Trace: " + e.StackTrace, 1, "Ok", "", "");
      }
    }




  }


}
