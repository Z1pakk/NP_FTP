using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FTPExampleWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string Username;
        private string Filename;
        private string Fullname;
        private string Server;
        private string Password;
        private string path;
        private string localdest;

        private BackgroundWorker bgWorker = new BackgroundWorker();
        public MainWindow()
        {
            InitializeComponent();
            if (cbDownload.IsChecked == true)
            {
                cbUpload.IsEnabled = false;
            }
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.ProgressChanged += BgWorker_ProgressChanged;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
            bgWorker.WorkerReportsProgress = true;
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (cbDownload.IsChecked == true)
            {
                lblStatus.Content = "Download Complete!";
            }

            if (cbUpload.IsChecked == true)
            {
                lblStatus.Content = "Upload Complete!";
            }
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            if (cbDownload.IsChecked == true)
            {
                lblStatus.Content = $"Downloaded {e.ProgressPercentage}%";
                pbProgress.Value = e.ProgressPercentage;
            }

            if (cbUpload.IsChecked == true)
            {
                lblStatus.Content = $"Uploaded {e.ProgressPercentage}%";
                pbProgress.Value = e.ProgressPercentage;
            }
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string url = string.Format("ftp://{0}/{1}", Server, Filename);
            FtpWebRequest mainRequest = (FtpWebRequest)WebRequest.Create(new Uri(url));
            if (this.Dispatcher.Invoke(()=>cbDownload.IsChecked == true))
            {
                
                mainRequest.Credentials = new NetworkCredential(Username, Password);
                mainRequest.Method = WebRequestMethods.Ftp.DownloadFile;  //Download Method

                // Get some data form the source file like the zise and the TimeStamp. every data you request need to be a different request and response
                FtpWebRequest request1 = (FtpWebRequest)WebRequest.Create(new Uri(url));
                request1.Credentials = new NetworkCredential(Username, Password);
                request1.Method = WebRequestMethods.Ftp.GetFileSize;  //GetFileze Method
                FtpWebResponse response = (FtpWebResponse)request1.GetResponse();
                double total = response.ContentLength;
                response.Close();

                FtpWebRequest request2 = (FtpWebRequest)WebRequest.Create(new Uri(url));
                request2.Credentials = new NetworkCredential(Username, Password);
                request2.Method = WebRequestMethods.Ftp.GetDateTimestamp; //GetTimestamp Method
                FtpWebResponse response2 = (FtpWebResponse)request2.GetResponse();
                DateTime modify = response2.LastModified;
                response2.Close();

                using (Stream ftpstream = mainRequest.GetResponse().GetResponseStream())
                {
                    using (FileStream fs = new FileStream(localdest, FileMode.Create))
                    {

                        // Method to calculate and show the progress.
                        byte[] buffer = new byte[1024];
                        int byteRead = 0;
                        double read = 0;
                        do
                        {
                            byteRead = ftpstream.Read(buffer, 0, 1024);
                            fs.Write(buffer, 0, byteRead);
                            read += (double)byteRead;
                            double percentage = read / total * 100;
                            bgWorker.ReportProgress((int)percentage);
                        }
                        while (byteRead != 0);

                    }
                }

            }
            else
            {
                mainRequest.Method = WebRequestMethods.Ftp.UploadFile;
                mainRequest.Credentials = new NetworkCredential(Username, Password);
                using (Stream ftpstream = mainRequest.GetRequestStream())
                {
                    using (FileStream fs = File.OpenRead(Fullname))
                    {

                        // Method to calculate and show the progress.
                        byte[] buffer = new byte[1024];
                        double total = (double)fs.Length;
                        int byteRead = 0;
                        double read = 0;
                        do
                        {
                            byteRead = fs.Read(buffer, 0, 1024);
                            ftpstream.Write(buffer, 0, byteRead);
                            read += (double)byteRead;
                            double percentage = read / total * 100;
                            bgWorker.ReportProgress((int)percentage);
                        }
                        while (byteRead != 0);
                    }
                }
            }
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            Username = txtUsername.Text;
            Password = txtPassword.Text;
            Server = txtServer.Text;

            if (cbUpload.IsChecked == true)
            {
                OpenFileDialog ofd = new OpenFileDialog() { Multiselect = true, ValidateNames = true, Filter = "All Files|*.*" };
                if (ofd.ShowDialog() == true)
                {
                    FileInfo fi = new FileInfo(ofd.FileName);
                    Filename = fi.Name;
                    Fullname = fi.FullName;
                }
            }


            if (cbDownload.IsChecked == true)
            {
                Filename = txtFileName.Text;
                path = @"H:\testFTP";
                localdest = path + @"/" + Filename;
                Fullname = Server + @"/" + Filename;
            }

            //Start the Background and wait a little to start it.
            bgWorker.RunWorkerAsync();  //the most important command to start the background worker
        }

        private void cbUpload_Checked(object sender, RoutedEventArgs e)
        {
            txtFileName.IsEnabled = false;
        }

        private void cbUpload_Unchecked(object sender, RoutedEventArgs e)
        {
            txtFileName.IsEnabled = true;
        }
    }
}
