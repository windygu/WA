﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AdvWebUIAPI;
using ThirdPartyToolControl;
using iATester;
using CommonFunction;

namespace CreateMap
{
    public partial class Form1 : Form, iATester.iCom
    {
        IAdvSeleniumAPI api;
        cThirdPartyToolControl tpc = new cThirdPartyToolControl();
        cEventLog EventLog = new cEventLog();

        private delegate void DataGridViewCtrlAddDataRow(DataGridViewRow i_Row);
        private DataGridViewCtrlAddDataRow m_DataGridViewCtrlAddDataRow;
        internal const int Max_Rows_Val = 65535;
        string baseUrl;
        string sIniFilePath = @"C:\WebAccessAutoTestSetting.ini";
        string slanguage;

        //Send Log data to iAtester
        public event EventHandler<LogEventArgs> eLog = delegate { };
        //Send test result to iAtester
        public event EventHandler<ResultEventArgs> eResult = delegate { };
        //Send execution status to iAtester
        public event EventHandler<StatusEventArgs> eStatus = delegate { };

        public void StartTest()
        {
            //Add test code
            long lErrorCode = 0;
            EventLog.AddLog("===Create map start (by iATester)===");
            if (System.IO.File.Exists(sIniFilePath))    // 再load一次
            {
                EventLog.AddLog(sIniFilePath + " file exist, load initial setting");
                InitialRequiredInfo(sIniFilePath);
            }
            EventLog.AddLog("Project= " + ProjectName.Text);
            EventLog.AddLog("WebAccess IP address= " + WebAccessIP.Text);
            lErrorCode = Form1_Load(ProjectName.Text, WebAccessIP.Text, TestLogFolder.Text, Browser.Text);
            EventLog.AddLog("===Create map end (by iATester)===");

            if (lErrorCode == 0)
                eResult(this, new ResultEventArgs(iResult.Pass));
            else
                eResult(this, new ResultEventArgs(iResult.Fail));

            eStatus(this, new StatusEventArgs(iStatus.Completion));
        }

        public Form1()
        {
            InitializeComponent();
            try
            {
                m_DataGridViewCtrlAddDataRow = new DataGridViewCtrlAddDataRow(DataGridViewCtrlAddNewRow);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            Browser.SelectedIndex = 0;
            if (System.IO.File.Exists(sIniFilePath))
            {
                EventLog.AddLog(sIniFilePath + " file exist, load initial setting");
                InitialRequiredInfo(sIniFilePath);
            }
        }

        long Form1_Load(string sProjectName, string sWebAccessIP, string sTestLogFolder, string sBrowser)
        {
            baseUrl = "http://" + sWebAccessIP;

            if (sBrowser == "Internet Explorer")
            {
                EventLog.AddLog("Browser= Internet Explorer");
                api = new AdvSeleniumAPI("IE", "");
                System.Threading.Thread.Sleep(1000);
            }
            else if (sBrowser == "Mozilla FireFox")
            {
                EventLog.AddLog("Browser= Mozilla FireFox");
                api = new AdvSeleniumAPI("FireFox", "");
                System.Threading.Thread.Sleep(1000);
            }


            // Launch Firefox and login
            api.LinkWebUI(baseUrl + "/broadWeb/bwconfig.asp?username=admin");
            api.ById("userField").Enter("").Submit().Exe();
            PrintStep("Login WebAccess");

            // Configure project by project name
            api.ByXpath("//a[contains(@href, '/broadWeb/bwMain.asp') and contains(@href, 'ProjName=" + sProjectName + "')]").Click();
            PrintStep("Configure project");

            //Create Map
            EventLog.AddLog("Create Map...");
            CreateMap(sTestLogFolder);
            PrintStep("Create Map");

            api.Quit();
            PrintStep("Quit browser");

            bool bSeleniumResult = true;
            int iTotalSeleniumAction = dataGridView1.Rows.Count;
            for (int i = 0; i < iTotalSeleniumAction - 1; i++)
            {
                DataGridViewRow row = dataGridView1.Rows[i];
                string sSeleniumResult = row.Cells[2].Value.ToString();
                if (sSeleniumResult != "pass")
                {
                    bSeleniumResult = false;
                    EventLog.AddLog("Test Fail !!");
                    EventLog.AddLog("Fail TestItem = " + row.Cells[0].Value.ToString());
                    EventLog.AddLog("BrowserAction = " + row.Cells[1].Value.ToString());
                    EventLog.AddLog("Result = " + row.Cells[2].Value.ToString());
                    EventLog.AddLog("ErrorCode = " + row.Cells[3].Value.ToString());
                    EventLog.AddLog("ExeTime(ms) = " + row.Cells[4].Value.ToString());
                    break;
                }
            }

            if (bSeleniumResult)
            {
                Result.Text = "PASS!!";
                Result.ForeColor = Color.Green;
                EventLog.AddLog("Test Result: PASS!!");
                return 0;
            }
            else
            {
                Result.Text = "FAIL!!";
                Result.ForeColor = Color.Red;
                EventLog.AddLog("Test Result: FAIL!!");
                return -1;
            }

            //return 0;
        }

        private void DataGridViewCtrlAddNewRow(DataGridViewRow i_Row)
        {
            if (this.dataGridView1.InvokeRequired)
            {
                this.dataGridView1.Invoke(new DataGridViewCtrlAddDataRow(DataGridViewCtrlAddNewRow), new object[] { i_Row });
                return;
            }

            this.dataGridView1.Rows.Insert(0, i_Row);
            if (dataGridView1.Rows.Count > Max_Rows_Val)
            {
                dataGridView1.Rows.RemoveAt((dataGridView1.Rows.Count - 1));
            }
            this.dataGridView1.Update();
        }

        private void CreateMap(string sTestLogFolder)
        {
            api.SwitchToCurWindow(0);
            api.SwitchToFrame("rightFrame", 0);

            if (slanguage == "CHS")     // fuck china special case..
            {
                api.ByXpath("//a[contains(@href, '/broadWeb/bmap/bmapcreate.asp?')]").Click();
                System.Threading.Thread.Sleep(2000);

                EventLog.AddLog("Click 'New Google Map' test");
                api.ByXpath("//a[contains(@href, '/broadWeb/gmap/gmapcreate.asp?')]").ClickAndWait(1000);
                EventLog.PrintScreen("CreateMapTest_GoogleMap");

                PrintStep("Google Map click test");
            }
            else
            {
                api.ByXpath("//a[contains(@href, '/broadWeb/gmap/gmapcreate.asp')]").Click();
                System.Threading.Thread.Sleep(2000);

                //TestGoogleMap/BaiduMap
                EventLog.AddLog("Click 'New Baidu Map' test");
                api.ByXpath("//a[contains(@href, '/broadWeb/bmap/bmapcreate.asp?')]").ClickAndWait(1000);
                EventLog.PrintScreen("CreateMapTest_BiaduMap");

                EventLog.AddLog("Click 'New Google Map' test");
                api.ByXpath("//a[contains(@href, '/broadWeb/gmap/gmapcreate.asp?')]").ClickAndWait(1000);
                EventLog.PrintScreen("CreateMapTest_GoogleMap");
                PrintStep("Google&Baidu Map click test");
            }

            //Excel-In sample map
            EventLog.AddLog("Excel in sample map");
            api.ByXpath("//a[contains(@href, 'gmaptoJsPg1.asp?pos=import')]").Click();
            string sCurrentFilePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(this.GetType()).Location);
            string sourceSampleFile = sCurrentFilePath + "\\MapSample\\MapSample.xls";
            string destWApath = @"C:\Inetpub\wwwroot\broadweb\gmap\MapSample.xls";
            System.IO.File.Copy(sourceSampleFile, destWApath, true);
            api.ByName("dataFileName").Clear();
            api.ByName("dataFileName").Enter("MapSample").Submit().Exe();
            api.ByName("act").Click();
            System.Threading.Thread.Sleep(2000);
            EventLog.PrintScreen("CreateMapTest_Import_SampleMap");
            PrintStep("Excel In Map");

            //Options
            EventLog.AddLog("Options setting...");
            EventLog.AddLog("Marker title font set");
            api.ByXpath("(//a[contains(@href, '#')])[4]").Click();
            System.Threading.Thread.Sleep(1000);
            api.ByXpath("(//input[@name='aa'])[2]").Click();
            System.Threading.Thread.Sleep(500);
            api.ByXpath("(//input[@name='aa'])[3]").Click();
            System.Threading.Thread.Sleep(500);
            api.ByXpath("(//input[@name='aa'])[1]").Click();
            System.Threading.Thread.Sleep(500);

            api.ByXpath("(//input[@name='bb'])[2]").Click();
            System.Threading.Thread.Sleep(500);
            api.ByXpath("(//input[@name='bb'])[1]").Click();
            System.Threading.Thread.Sleep(500);

            api.ByXpath("//input[@id='cc']").Click();
            api.ByXpath("//div[@id='fontpicker']/div").Click(); //Font Family = "Microsoft YaHei"
            System.Threading.Thread.Sleep(500);
            api.ByXpath("//select[@id='dd']").Click();
            api.ByXpath("//select[@id='dd']").Enter("16").Exe();   //Font Size = 16
            System.Threading.Thread.Sleep(500);
            //api.ById("ee").Clear();
            //api.ById("ee").Enter("FF0000").Exe();   //Title Color = RED
            //System.Threading.Thread.Sleep(1000);
            PrintStep("Marker Title Font setting");

            EventLog.AddLog("Marker label font set");
            api.ByXpath("//input[@id='ff']").Click();
            api.ByXpath("//div[@id='fontpicker']/div[10]").Click(); //Font Family = "Impact"
            System.Threading.Thread.Sleep(500);
            api.ByXpath("//select[@id='gg']").Click();
            api.ByXpath("//select[@id='gg']").Enter("16").Exe();   //Font Size = 16
            System.Threading.Thread.Sleep(500);
            api.ById("hh").Clear();
            api.ById("hh").Enter("0000FF").Exe();   //Title Color = Bule
            System.Threading.Thread.Sleep(500);

            api.ById("ee").Clear();
            api.ById("ee").Enter("FF00EE").Exe();   //Title Color = Purple
            System.Threading.Thread.Sleep(500);

            api.ByXpath("//div[@id='opt']/div[27]/input").Click();
            System.Threading.Thread.Sleep(500);
            PrintStep("Marker Label Font");

            //Save
            EventLog.AddLog("Save map");
            api.ByXpath("(//a[contains(@href, '#')])[2]").Click();
            System.Threading.Thread.Sleep(1000);
            SendKeys.SendWait("{ENTER}");
            System.Threading.Thread.Sleep(1000);
            PrintStep("Save");
            EventLog.PrintScreen("CreateMapTest_ModifiedMap");
            System.Threading.Thread.Sleep(1000);

            //Excel-Out
            EventLog.AddLog("Excel out modified map");
            api.ByXpath("//a[contains(@href, 'gmaptoJsPg1.asp?pos=export')]").Click();
            api.ByName("chk").Click();
            api.ByName("dataFileName").Clear();
            api.ByName("dataFileName").Enter("gmap_"+ DateTime.Now.ToString("yyyyMMdd")).Submit().Exe();
            api.ByName("act").Click();
            PrintStep("Excel Out Map");

            try
            {
                string sourceFile = @"C:\Inetpub\wwwroot\broadweb\gmap\gmap_" + DateTime.Now.ToString("yyyyMMdd") + ".xls";
                string destFile = sTestLogFolder + "\\CreateMapTest_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".xls";
                EventLog.AddLog("Copy export file form " + sourceFile + " to " + destFile);
                System.IO.File.Copy(sourceFile, destFile, true);
                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                EventLog.AddLog(ex.ToString());
            }

            //Delete
            EventLog.AddLog("Delete map");
            api.ByXpath("(//a[contains(@href, '#')])[3]").Click();
            System.Threading.Thread.Sleep(1000);
            api.Accept();
            System.Threading.Thread.Sleep(1000);
            PrintStep("Delete");
        }

        private void ReturnSCADAPage()
        {
            api.SwitchToCurWindow(0);
            api.SwitchToFrame("leftFrame", 0);
            api.ByXpath("//a[contains(@href, '/broadWeb/bwMainRight.asp') and contains(@href, 'name=TestSCADA')]").Click();

        }

        private void Start_Click(object sender, EventArgs e)
        {
            long lErrorCode = 0;
            EventLog.AddLog("===Create map start===");
            CheckifIniFileChange();
            EventLog.AddLog("Project= " + ProjectName.Text);
            EventLog.AddLog("WebAccess IP address= " + WebAccessIP.Text);
            lErrorCode = Form1_Load(ProjectName.Text, WebAccessIP.Text, TestLogFolder.Text, Browser.Text);
            EventLog.AddLog("===Create map end===");
        }

        private void PrintStep(string sTestItem)
        {
            DataGridViewRow dgvRow;
            DataGridViewCell dgvCell;

            var list = api.GetStepResult();
            foreach (var item in list)
            {
                AdvSeleniumAPI.ResultClass _res = (AdvSeleniumAPI.ResultClass)item;
                //
                dgvRow = new DataGridViewRow();
                if (_res.Res == "fail")
                    dgvRow.DefaultCellStyle.ForeColor = Color.Red;
                dgvCell = new DataGridViewTextBoxCell(); //Column Time
                //
                if (_res == null) continue;
                //
                dgvCell.Value = sTestItem;
                dgvRow.Cells.Add(dgvCell);
                //
                dgvCell = new DataGridViewTextBoxCell();
                dgvCell.Value = _res.Decp;
                dgvRow.Cells.Add(dgvCell);
                //
                dgvCell = new DataGridViewTextBoxCell();
                dgvCell.Value = _res.Res;
                dgvRow.Cells.Add(dgvCell);
                //
                dgvCell = new DataGridViewTextBoxCell();
                dgvCell.Value = _res.Err;
                dgvRow.Cells.Add(dgvCell);
                //
                dgvCell = new DataGridViewTextBoxCell();
                dgvCell.Value = _res.Tdev;
                dgvRow.Cells.Add(dgvCell);

                m_DataGridViewCtrlAddDataRow(dgvRow);
            }
            Application.DoEvents();
        }

        private void InitialRequiredInfo(string sFilePath)
        {
            StringBuilder sDefaultUserLanguage = new StringBuilder(255);
            StringBuilder sDefaultProjectName1 = new StringBuilder(255);
            StringBuilder sDefaultProjectName2 = new StringBuilder(255);
            StringBuilder sDefaultIP1 = new StringBuilder(255);
            StringBuilder sDefaultIP2 = new StringBuilder(255);
            /*
            tpc.F_WritePrivateProfileString("ProjectName", "Ground PC or Primary PC", "TestProject", @"C:\WebAccessAutoTestSetting.ini");
            tpc.F_WritePrivateProfileString("ProjectName", "Cloud PC or Backup PC", "CTestProject", @"C:\WebAccessAutoTestSetting.ini");
            tpc.F_WritePrivateProfileString("IP", "Ground PC or Primary PC", "172.18.3.62", @"C:\WebAccessAutoTestSetting.ini");
            tpc.F_WritePrivateProfileString("IP", "Cloud PC or Backup PC", "172.18.3.65", @"C:\WebAccessAutoTestSetting.ini");
            */
            tpc.F_GetPrivateProfileString("UserInfo", "Language", "NA", sDefaultUserLanguage, 255, sFilePath);
            tpc.F_GetPrivateProfileString("ProjectName", "Ground PC or Primary PC", "NA", sDefaultProjectName1, 255, sFilePath);
            tpc.F_GetPrivateProfileString("ProjectName", "Cloud PC or Backup PC", "NA", sDefaultProjectName2, 255, sFilePath);
            tpc.F_GetPrivateProfileString("IP", "Ground PC or Primary PC", "NA", sDefaultIP1, 255, sFilePath);
            tpc.F_GetPrivateProfileString("IP", "Cloud PC or Backup PC", "NA", sDefaultIP2, 255, sFilePath);
            slanguage = sDefaultUserLanguage.ToString();    // 在這邊讀取使用語言
            ProjectName.Text = sDefaultProjectName1.ToString();
            WebAccessIP.Text = sDefaultIP1.ToString();
        }

        private void CheckifIniFileChange()
        {
            StringBuilder sDefaultProjectName1 = new StringBuilder(255);
            StringBuilder sDefaultProjectName2 = new StringBuilder(255);
            StringBuilder sDefaultIP1 = new StringBuilder(255);
            StringBuilder sDefaultIP2 = new StringBuilder(255);
            if (System.IO.File.Exists(sIniFilePath))    // 比對ini檔與ui上的值是否相同
            {
                EventLog.AddLog(".ini file exist, check if .ini file need to update");
                tpc.F_GetPrivateProfileString("ProjectName", "Ground PC or Primary PC", "NA", sDefaultProjectName1, 255, sIniFilePath);
                tpc.F_GetPrivateProfileString("ProjectName", "Cloud PC or Backup PC", "NA", sDefaultProjectName2, 255, sIniFilePath);
                tpc.F_GetPrivateProfileString("IP", "Ground PC or Primary PC", "NA", sDefaultIP1, 255, sIniFilePath);
                tpc.F_GetPrivateProfileString("IP", "Cloud PC or Backup PC", "NA", sDefaultIP2, 255, sIniFilePath);

                if (ProjectName.Text != sDefaultProjectName1.ToString())
                {
                    tpc.F_WritePrivateProfileString("ProjectName", "Ground PC or Primary PC", ProjectName.Text, sIniFilePath);
                    EventLog.AddLog("New ProjectName update to .ini file!!");
                    EventLog.AddLog("Original ini:" + sDefaultProjectName1.ToString());
                    EventLog.AddLog("New ini:" + ProjectName.Text);
                }
                if (WebAccessIP.Text != sDefaultIP1.ToString())
                {
                    tpc.F_WritePrivateProfileString("IP", "Ground PC or Primary PC", WebAccessIP.Text, sIniFilePath);
                    EventLog.AddLog("New WebAccessIP update to .ini file!!");
                    EventLog.AddLog("Original ini:" + sDefaultIP1.ToString());
                    EventLog.AddLog("New ini:" + WebAccessIP.Text);
                }
            }
            else
            {
                EventLog.AddLog(".ini file not exist, create new .ini file. Path: " + sIniFilePath);
                tpc.F_WritePrivateProfileString("ProjectName", "Ground PC or Primary PC", ProjectName.Text, sIniFilePath);
                tpc.F_WritePrivateProfileString("ProjectName", "Cloud PC or Backup PC", "CTestProject", sIniFilePath);
                tpc.F_WritePrivateProfileString("IP", "Ground PC or Primary PC", WebAccessIP.Text, sIniFilePath);
                tpc.F_WritePrivateProfileString("IP", "Cloud PC or Backup PC", "172.18.3.65", sIniFilePath);
            }
        }
    }
}
