using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Ionic.Zip;
using AutoUpdate;
namespace DownLoadForm
{
    delegate void InvokText(TextBox t,string str);
    public partial class DownLoadForm : Form
    {
        private string StartPath = System.Windows.Forms.Application.StartupPath;

        private string DownLoadFilePath = "Temp";

        private string DownLoadFileFullPath;

        private string UnZipFilePath;

        private string UnZipFileFullPath;

        private string RestartPath;

        private string RestartFullPath;

        /// <summary>
        /// Download data uri
        /// </summary>
        private string Download_Uri;

        /// <summary>
        /// Download data file name
        /// </summary>
        private string Download_FileName;
        
        /// <summary>
        /// Download data file size
        /// </summary>
        private long Down_load_FileSize = 0;


        private bool IsUpdate = false;



        internal DownLoadForm(string[] sender)
        {
            InitializeComponent();
            
            RestartFullPath = sender[0];
            Download_Uri = sender[1];
            Download_FileName = sender[2];

            RestartPath = Path.GetDirectoryName(RestartFullPath);
            DownLoadFilePath = Path.Combine(StartPath, DownLoadFilePath);
            DownLoadFileFullPath = Path.Combine(DownLoadFilePath, Download_FileName);
            
            UnZipFilePath = DownLoadFilePath;
            UnZipFileFullPath = DownLoadFileFullPath.Replace(".zip", "");
        }
        private void TestVariable()
        {
            MessageBox.Show(string.Format("RestartFullPath = {0}", RestartFullPath));
            MessageBox.Show(string.Format("Download_Uri = {0}", Download_Uri));
            MessageBox.Show(string.Format("Download_FileName = {0}", Download_FileName));
            MessageBox.Show(string.Format("DownLoadFilePath = {0}", DownLoadFilePath));
            MessageBox.Show(string.Format("DownLoadFileFullPath = {0}", DownLoadFileFullPath));
            MessageBox.Show(string.Format("UnZipFilePath = {0}", UnZipFilePath));
            MessageBox.Show(string.Format("UnZipFileFullPath = {0}", UnZipFileFullPath));
        }
        private void DownLoadForm_Load(object sender, EventArgs e)
        {
                //TestVariable();
                DownLoadFile();
        }
        
        private void DownLoadFile()
        {
            WebClient DownloadClient;
            HttpWebRequest  Httpreques;
            HttpWebResponse Httpresponse;
            ServicePointManager.ServerCertificateValidationCallback =
                        (sender, cert, chain, sslPolicyErrors) => true;//ignore ssl Certificate
            DownloadClient = new WebClient();
            DownloadClient.DownloadProgressChanged += DownloadProgressChanged;
            DownloadClient.DownloadFileCompleted += DownloadCompletedEventHandler;
            try
            {
                DisplayMission("下載更新檔\n");
                Httpreques  = (HttpWebRequest)HttpWebRequest.Create(Download_Uri);
                Httpresponse = (HttpWebResponse)Httpreques.GetResponse();
                Down_load_FileSize = Httpresponse.ContentLength;
                if (!Directory.Exists(DownLoadFilePath))
                    Directory.CreateDirectory(DownLoadFilePath);
                
                lab_FiileName.Text = Download_FileName;
                DownloadClient.DownloadFileAsync(new Uri(Download_Uri),
                    DownLoadFileFullPath);
            }
            catch(Exception ex)
            {
                //AutoUpdate.AutoUpdate.Debug_Error("下載失敗");
                AutoUpdate.AutoUpdate.Debug_Error(ex.ToString());
            }
        
        }
        
        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            string DisplayBytes = FormatBytes(e.BytesReceived, 1, false) + "/" + FormatBytes(Down_load_FileSize, 1, true);
            lab_FileSize.Text = DisplayBytes;
            bar_rate.Value = (int)((e.ProgressPercentage) * 0.8);

        }
        
        void DownloadCompletedEventHandler(object sender, AsyncCompletedEventArgs e)
        {
            IsUpdate = true;
            btn_Cancel.Text = "離開";
            lab_Title.Text = "更新檔下載完成";
            BackgroundWorker unzip_bg = new BackgroundWorker();
            unzip_bg.DoWork += new DoWorkEventHandler(unzip);
            unzip_bg.RunWorkerCompleted += new RunWorkerCompletedEventHandler(unzip_compelete);
            unzip_bg.RunWorkerAsync();
        }
        
        private void unzip(object sender, DoWorkEventArgs e)
        {
            DisplayMission("更新檔解壓縮");
            lab_Title.Text = "更新檔解壓縮";
            UnZipFiles(DownLoadFileFullPath,UnZipFilePath, null);
        
        }

        private void unzip_compelete(object sender, RunWorkerCompletedEventArgs e)
        {
            bar_rate.Value = 90;
            BackgroundWorker mvFile_bg = new BackgroundWorker();
            mvFile_bg.DoWork += new DoWorkEventHandler(mvFile);
            mvFile_bg.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mvFile_compelete);
            mvFile_bg.RunWorkerAsync();

        }
        
        private void mvFile(object sender, DoWorkEventArgs e)
        {
            DisplayMission("安裝更新檔");
            lab_Title.Text = "安裝更新檔";
            DirectoryInfo source =new DirectoryInfo(UnZipFileFullPath);
            DirectoryInfo dir =new DirectoryInfo(RestartPath);
            try
            {
                CopyFilesRecursively(source, dir);
            }
            catch (Exception ex) {

                AutoUpdate.AutoUpdate.Debug_Error(ex.ToString());
            }
        }
      
        private void mvFile_compelete(object sender, RunWorkerCompletedEventArgs e)
        {
            bar_rate.Value = 100;
            lab_Title.Text = "更新完成";
            btn_Cancel.Text = "完成";
            File.Delete(DownLoadFileFullPath);
            Directory.Delete(UnZipFileFullPath, true);
        }

        private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name),true);
        }

        //解壓縮檔案
        //path: 解壓縮檔案目錄路徑
        //password: 密碼
        private static void UnZipFiles(string soruce,string dir ,string password)
        {
            ZipFile unzip = ZipFile.Read(soruce);
            if (password != null && password != string.Empty) unzip.Password = password;

            foreach (ZipEntry e in unzip)
            {
                e.Extract(dir, ExtractExistingFileAction.OverwriteSilently);
            }
            unzip.Dispose();
            GC.Collect();
        }


        private string FormatBytes(long bytes, int decimalPlaces, bool showByteType)
        {
            double newBytes = bytes;
            string formatString = "{0";
            string byteType;

            // Check if best size in KB
            if (newBytes > 1024 && newBytes < 1048576)
            {
                newBytes /= 1024;
                byteType = "KB";
            }
            else if (newBytes > 1048576 && newBytes < 1073741824)
            {
                // Check if best size in MB
                newBytes /= 1048576;
                byteType = "MB";
            }
            else if (newBytes > 1073741824)
            {
                // Best size in GB
                newBytes /= 1073741824;
                byteType = "GB";
            }
            else
                byteType = "B";

            // Show decimals
            if (decimalPlaces > 0)
                formatString += ":0.";

            // Add decimals
            for (int i = 0; i < decimalPlaces; i++)
                formatString += "0";

            // Close placeholder
            formatString += "}";

            // Add byte type
            if (showByteType)
                formatString += byteType;

            return string.Format(formatString, newBytes);
        }

        private void btn_Cancel_Click(object sender, EventArgs e)
        {
            if (!this.IsUpdate)
                Application.Exit();
            else
            {
                try
                {
                    using (Process p = new Process())
                    {
                        p.StartInfo.FileName = RestartFullPath;
                        p.Start();
                    }
                    Application.Exit();
                }
                catch
                {
                    MessageBox.Show("Setting Tool 開啟失敗");
                }
            }
        }

        private void btn_detail_Click(object sender, EventArgs e)
        {
            txt_detail.Visible = !txt_detail.Visible;
        }

        void ChangText(TextBox t, string str)
        {
            t.Text += str;
            t.Text += "\r\n";
        }
        private void DisplayMission(string str)
        {
            InvokText display;
            display = new InvokText(ChangText);
            display.Invoke(txt_detail,str);
        }


    }
}
