using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using Microsoft.ConfigurationManagement.AdminConsole;
using Microsoft.ConfigurationManagement.AdminConsole.Schema;
using Microsoft.ConfigurationManagement.ManagementProvider;
using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;


using System.Globalization;
using System.Xml.Linq;
using System.Linq;

namespace SystemCenter.ConfigurationManager.ActionHelper
{
    public static class MultiSelectActionHelper
    {

        #region Properties

            public static string Parameters { 
                get{ 
                    return _parameters.Replace("##SUB:Name##",subName) ;
                } 
                set{ _parameters = value;} 
            }

            public static string FilePath { get; set; }

        #endregion

        #region Internals 
            
            static string _parameters = string.Empty;
            static string _fiePath = string.Empty;

        #endregion

        #region Constants

            const string xmlStorageActionFolder = @"\XmlStorage\Extensions\Actions";

        #endregion

        #region Global Variabels

            private static string subName = string.Empty;

        #endregion

        /// <summary>
        /// Executes a multi select action
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="scopeNode"></param>
        /// <param name="action"></param>
        /// <param name="resultObject"></param>
        /// <param name="dataUpdatedDelegate"></param>
        /// <param name="status"></param>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sender")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "scopeNode")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "action")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "dataUpdatedDelegate")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "status")]
        public static void ExecuteMultiSelectAction(object sender, ScopeNode scopeNode, ActionDescription action, IResultObject resultObject, PropertyDataUpdated dataUpdatedDelegate, Status status)
        {

            if (resultObject.Count > 0) { subName = resultObject.DisplayString; }
            GetExecutableActionByName(action.DisplayName);

        }

        /*
        public static bool ShowMultiSelectAction(object sender, ScopeNode scopeNode, ActionDescription action, ResultObjectBase resultObject)
        {
            return false;
        }
        */

        /// <summary>
        /// Searches for an executable action 
        /// </summary>
        /// <param name="MnemonicDisplayName">MnemonicDisplayName of the action to look for</param>
        public static void GetExecutableActionByName(string MnemonicDisplayName)
        {

           DirectoryInfo di = new DirectoryInfo(GetConfigMgrInstallDirectoryPath() + xmlStorageActionFolder);
           foreach (var fi in di.EnumerateFiles("*.xml",SearchOption.AllDirectories))
           {

               XmlDocument xmlDoc = new XmlDocument();
               xmlDoc.Load(fi.OpenRead());

               XmlNodeList xmlNodeList = xmlDoc.SelectNodes(String.Format(@"/ActionDescription/ActionGroups/ActionDescription[@DisplayName='{0}']",MnemonicDisplayName));


               // Get all descendant elements with the specified value as value if attribute displayname
               XDocument xd = XDocument.Load(fi.OpenRead());
               IEnumerable<XElement> xmlElementList =
                from el in xd.Descendants()
                where el.Attribute("DisplayName") != null
                && el.Attribute("DisplayName").Value == MnemonicDisplayName
                select el;

               // Loop through every element and find an executable action
               foreach (var elItem in xmlElementList)
               {
                   // if executable action
                   if(elItem.Attribute("Class").Value == "Executable")
                   {

                       // Store the action details and invoke the process
                       FilePath = (String.IsNullOrEmpty(elItem.Element("Executable").Element("FilePath").Value)) ? string.Empty : elItem.Element("Executable").Element("FilePath").Value;
                       Parameters = (String.IsNullOrEmpty(elItem.Element("Executable").Element("Parameters").Value)) ? string.Empty : elItem.Element("Executable").Element("Parameters").Value;

                       ExecuteProcess(FilePath, Parameters, false);

                    }
  
               }
        
           } 

        }

        /// <summary>
        /// Execute a process
        /// </summary>
        /// <param name="filePath">File to the executable</param>
        /// <param name="parameters">Process arguments</param>
        /// <param name="wait">Wait for completion of process</param>
        public static void ExecuteProcess(string filePath, string parameters, bool wait)
        {

            try
            {
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo(filePath, parameters);
                process.Start();
                if (wait) { process.WaitForExit(); };
                Debug.WriteLine(String.Format("Process started: {0} {1} ", filePath, parameters));
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
   

      
        }

        /// <summary>
        /// Gets the installation Directory of the ConfigMgr Console
        /// </summary>
        /// <returns>Path to ConfigMgr Console installation directory</returns>
        public static string GetConfigMgrInstallDirectoryPath()
        {

            try
            {
                string configMgrInstallDirectoryPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\ConfigMgr10\Setup", "UI Installation Directory", null).ToString();

                return configMgrInstallDirectoryPath;
            }
            catch (Exception)
            {
                
                throw;
            }
            
        }


    }
}
