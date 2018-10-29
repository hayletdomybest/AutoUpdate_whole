﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace AutoUpdate
{
    public delegate void _UpdateComplete(bool update);
    public class AutoUpdate
    {
        public _UpdateComplete UpdateComplete = null; 

        /// <summary>
        /// Thread to find update
        /// </summary>
        private BackgroundWorker BgWorker;

        private UpdateInfo ServerUpdateInfo;

        /// <summary>
        /// Uri of the update xml on the server
        /// </summary>
        private Uri UpdateXmlServer;


        /// <summary>
        /// Version of Local
        /// </summary>
        private Version LocalVersion;

        /// <summary>
        /// Creates a new AutoUpdate object
        /// </summary>
        /// <param name="a">Parent ssembly to be attached</param>
        /// <param name="owner">Parent form to be attached</param>
        /// <param name="XMLOnServer">Uri of the update xml on the server</param>
        public AutoUpdate(Uri server, Version location)
        {
            UpdateXmlServer = server;
            LocalVersion = location;

            // Set up backgroundworker
            BgWorker = new BackgroundWorker();
            BgWorker.DoWork += new DoWorkEventHandler(BgWorker_DoWork);
            BgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BgWorker_RunWorkerCompleted);
        }

        /// <summary>
        /// Update data
        /// </summary>
        public void DoUpdate()
        {
            if (!BgWorker.IsBusy)
                BgWorker.RunWorkerAsync();
        }


        /// <summary>
        /// Checks for/parses update.xml on server
        /// </summary>
        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Check for update on server
            e.Cancel = (!AutoUpdateXml.IsExistServer(UpdateXmlServer));

            if (e.Cancel)
                return;

            ServerUpdateInfo = AutoUpdateXml.XmlParse(UpdateXmlServer);

        }


        /// <summary>
        /// After the background worker is done, prompt to update if there is one
        /// </summary>
        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool IsUpdate = false;
            // If there is a file on the server
            if (e.Cancelled)
                return;

            
            if (IsNeedUpdate(ServerUpdateInfo._Version,LocalVersion))
            {
                AcceptForm acceptform = new AcceptForm(ServerUpdateInfo);
                acceptform.ShowDialog();
                IsUpdate = (acceptform.DialogResult == DialogResult.Yes);
            }
            if (UpdateComplete != null)
                UpdateComplete(IsUpdate);
            
        }

        /// <summary>
        /// Check version if need update return true 
        /// </summary>
        /// <returns>True: Need update False :Not need</returns>
        public  bool IsNeedUpdate(Version Server, Version local)
        {
            return (Server > local);
        }


        /// <summary>
        /// Start Download application
        /// </summary>
        /// <param name="path">Download application path</param>
        /// <param name="openPath">Restart Application path</param>
        public void StartDownLoad(string start)
        {
            //Current process file address
            string cur_process = System.Windows.Forms.Application.ExecutablePath;

            //Parameter for passing to DownloadForm 
            ArgsBuilder args = new ArgsBuilder();

            //For start process application information 
            ProcessStartInfo pInfo = new ProcessStartInfo(start);

            //Parameter[1] = current process name
            args.Add(cur_process);

            args.Add(ServerUpdateInfo._Uri.ToString());

            args.Add(ServerUpdateInfo._FileName);

            pInfo.Arguments = args.ToString();
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo = pInfo;
                    p.Start();
                }
            }
            catch {
                Debug_Error("下載程式路徑錯誤");
            }
            Application.Exit();
        }


        public static void Debug_Error(string err)
        {
            MessageBox.Show(err);
            Application.Exit();
        }
    }
}
