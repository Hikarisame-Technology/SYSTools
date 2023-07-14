﻿using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;
using System.Xml;
using SYSTools.Dialog;

namespace SYSTools.Pages
{
    /// <summary>
    /// Test.xaml 的交互逻辑
    /// </summary>
    public partial class Test : System.Windows.Controls.Page
    {
        public Test()
        {
            InitializeComponent();
        }
       
        private void HardwareTest()
        {
            ManagementObjectSearcher OpSystem = new ManagementObjectSearcher("SELECT * FROM win32_OperatingSystem");
            ManagementObjectCollection OpS = OpSystem.Get();

            ManagementObjectSearcher cmsystem = new ManagementObjectSearcher("SELECT * FROM win32_computersystem");
            ManagementObjectCollection cms = cmsystem.Get();

            ManagementObjectSearcher Process = new ManagementObjectSearcher("SELECT * FROM win32_Processor");
            ManagementObjectCollection CPUPr = Process.Get();

            ManagementObjectSearcher NetAdapter = new ManagementObjectSearcher(@"SELECT * FROM Win32_NetworkAdapter WHERE Manufacturer != 'Microsoft' AND NOT PNPDeviceID LIKE 'ROOT\\%'");
            ManagementObjectCollection NetAdapterDevice = NetAdapter.Get();

            ManagementObjectSearcher NETconfig = new ManagementObjectSearcher("SELECT * FROM win32_NetworkAdapterConfiguration WHERE IPEnabled = True");
            ManagementObjectCollection MNetconfig = NETconfig.Get();

            ManagementObjectSearcher Sound = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
            ManagementObjectCollection SoundDevice = Sound.Get();

            ManagementObjectSearcher Video = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            ManagementObjectCollection VideoDevice = Video.Get();
            ManagementObjectCollection DeskTopCont = Video.Get();

            ManagementObjectSearcher Monitor = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE service='monitor'");
            ManagementObjectCollection MonitorService = Monitor.Get();

            XmlWriterSettings XmlSettings = new XmlWriterSettings();
            XmlSettings.Indent = true;

            try
            {
                using (XmlWriter Xml_Writer = XmlWriter.Create("Info.xml", XmlSettings))
                {
                    Xml_Writer.WriteStartDocument();
                    Xml_Writer.WriteStartElement("Info");
                    Xml_Writer.WriteStartElement("ComputerSystem");

                    Xml_Writer.WriteStartElement("Attribute", "计算机信息");
                    foreach (ManagementObject cmsys in cms)
                    {
                        double memory = Convert.ToDouble(cmsys.GetPropertyValue("totalphysicalmemory")) / 1024 / 1024 / 1024;
                        memory = (int)memory;

                        Xml_Writer.WriteElementString("Properties", "工作组:" + Convert.ToChar(32) + cmsys.GetPropertyValue("domain").ToString());
                        Xml_Writer.WriteElementString("Properties", "计算机名称:" + Convert.ToChar(32) + cmsys.GetPropertyValue("__server").ToString());
                        Xml_Writer.WriteElementString("Properties", "计算机制造商:" + Convert.ToChar(32) + cmsys.GetPropertyValue("manufacturer").ToString());
                        Xml_Writer.WriteElementString("Properties", "计算机内存:" + Convert.ToChar(32) + (memory + 1).ToString() + "GB");
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteStartElement("Attribute", "操作系统信息");
                    foreach (ManagementObject OpSys in OpS)
                    {
                        Xml_Writer.WriteElementString("Properties", "Windows版本:" + Convert.ToChar(32) + OpSys.GetPropertyValue("Caption").ToString());
                        Xml_Writer.WriteElementString("Properties", "系统位数:" + Convert.ToChar(32) + OpSys.GetPropertyValue("OSArchitecture").ToString() + "操作系统");
                        Xml_Writer.WriteElementString("Properties", "内核版本号:" + Convert.ToChar(32) + OpSys.GetPropertyValue("Version").ToString());
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteStartElement("Attribute", "CPU信息");
                    foreach (ManagementObject CPU in CPUPr)
                    {
                        Xml_Writer.WriteElementString("Properties", "CPU型号:" + Convert.ToChar(32) + CPU.GetPropertyValue("Name").ToString() + Convert.ToChar(32) + CPU.GetPropertyValue("NumberOfCores").ToString() + "核" + CPU.GetPropertyValue("NumberOfLogicalProcessors").ToString() + "线程");
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteStartElement("Attribute", "显卡信息");
                    foreach (ManagementObject VideoDevice_Object in VideoDevice)
                    {
                        Xml_Writer.WriteElementString("Properties", VideoDevice_Object.GetPropertyValue("Name").ToString());
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteStartElement("Attribute", "网卡信息");
                    foreach (ManagementObject NetAdapter_Object in NetAdapterDevice)
                    {
                        Xml_Writer.WriteElementString("Properties", NetAdapter_Object.GetPropertyValue("Name").ToString());
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteStartElement("Attribute", "本机IP/MAC信息");
                    foreach (ManagementObject MNF in MNetconfig)
                    {
                        Xml_Writer.WriteElementString("Properties", "网卡名称:" + Convert.ToChar(32) + MNF.GetPropertyValue("Description").ToString());
                        Xml_Writer.WriteElementString("Properties", "MAC地址:" + Convert.ToChar(32) + MNF.GetPropertyValue("MACAddress").ToString());
                        Xml_Writer.WriteElementString("Properties", "本机IPv4/6地址:");
                        string[] ipAddresses = (string[])MNF["IPAddress"];
                        if (ipAddresses != null && ipAddresses.Length > 0)
                        {
                            foreach (string ipAddress in ipAddresses)
                            {
                                Xml_Writer.WriteElementString("Properties", Convert.ToChar(9) + ipAddress);
                            }
                        }
                        else
                        {
                            Xml_Writer.WriteElementString("Properties", Convert.ToChar(9) + "No IP Addresses");
                        }
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteStartElement("Attribute", "声卡信息");
                    foreach (ManagementObject SoundDevice_Object in SoundDevice)
                    {
                        Xml_Writer.WriteElementString("Properties", SoundDevice_Object.GetPropertyValue("Caption").ToString());
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteStartElement("Attribute", "显示器信息 (笔记本可获得内置屏幕型号 一般为MONITOR后的英文+数字结构)");
                    foreach (ManagementObject Monitor_Object in MonitorService)
                    {
                        Xml_Writer.WriteElementString("Properties", "显示器名称: " + Monitor_Object.GetPropertyValue("Name").ToString());
                        string[] hardwareIDs = Monitor_Object.GetPropertyValue("HardwareID") as string[];
                        string firstHardwareID = hardwareIDs?.FirstOrDefault();
                        Xml_Writer.WriteElementString("Properties", "显示屏硬件ID: " + firstHardwareID.ToString());
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteStartElement("Attribute", "屏幕分辨率.颜色深度.当前刷新率(有空位表示双卡或扩展未使用)");
                    foreach (ManagementObject DeskTop_Info in DeskTopCont)
                    {
                        try
                        {
                            Xml_Writer.WriteElementString("Properties", DeskTop_Info.GetPropertyValue("Name") + ": " + DeskTop_Info.GetPropertyValue("VideoModeDescription").ToString() + Convert.ToChar(32) + DeskTop_Info.GetPropertyValue("CurrentRefreshRate").ToString() + "Hz");
                        }
                        catch (NullReferenceException)
                        {
                            Xml_Writer.WriteElementString("Properties", DeskTop_Info.GetPropertyValue("Name") + ": " + "屏幕未接入");
                        }
                    }
                    Xml_Writer.WriteEndElement();

                    Xml_Writer.WriteEndElement();
                    Xml_Writer.WriteEndElement();
                    Xml_Writer.WriteEndDocument();
                }

                TestConfigDialog dialog = new TestConfigDialog();
                dialog.ShowAsync();
            }
            catch (Exception)
            {
                TestErrorDialog dialog = new TestErrorDialog();
                dialog.ShowAsync();

                using (XmlWriter Xml_Writer = XmlWriter.Create("Info.xml", XmlSettings))
                {
                    Xml_Writer.WriteStartDocument();
                    Xml_Writer.WriteStartElement("Info");
                    Xml_Writer.WriteStartElement("ComputerSystem");
                    Xml_Writer.WriteStartElement("Attribute", "配置提取失败 请重试或联系开发者");
                    Xml_Writer.WriteEndElement();
                    Xml_Writer.WriteEndElement();
                    Xml_Writer.WriteEndElement();
                    Xml_Writer.WriteEndDocument();
                }
            }
        }

        private bool Boo_FileExist(string Str_File)
        {
            return File.Exists(Str_File);
        }

        private void TestView_Loaded(object sender, RoutedEventArgs e)
        {
            string FileLoad = AppDomain.CurrentDomain.BaseDirectory;
            if (Boo_FileExist(FileLoad + "Info.xml"))
            {
                Xml();
            }
            else
            {
                TestView.Items.Clear();
                TestView.Items.Add("请点击检测配置按钮以获取最新配置信息");
            }
        }

        private void Xml()
        {
            TestView.Items.Clear();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Info.xml");
            XmlNode xn = xmlDoc.SelectSingleNode("//ComputerSystem");
            XmlNodeList xnl = xn.ChildNodes;
            XmlNode xnf;
            XmlElement root = xmlDoc.DocumentElement;
            XmlNodeList elemList = root.GetElementsByTagName("Attribute");
            for (int i = 0; i < elemList.Count; i++)
            {
                xnf = xnl.Item(i);
                XmlElement xe = (XmlElement)xnf;
                XmlNodeList xnf1 = xe.ChildNodes;
                TestView.Items.Add(xnf.Attributes["xmlns"].Value);
                foreach (XmlNode xnfItem in xnf1)
                {
                    TestView.Items.Add("    " + xnfItem.InnerText);
                }
            }
        }

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            TestConfigDialog dialog = new TestConfigDialog();
            dialog.ShowAsync();
        }

        private void TestBotton_Click(object sender, RoutedEventArgs e)
        {
            HardwareTest();
            Xml();
        }
    }
}
