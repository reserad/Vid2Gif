using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExtractor;
using System.Drawing.Imaging;
using System.Net;
using Gif.Components;

namespace Vid2Gif
{
    public partial class Form1 : Form
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public Form1()
        {
            InitializeComponent();
            MaximizeBox = false;
            MinimizeBox = true;
        }

        public struct Video
        {
            public string Title;
            public string VideoExtension;
        }

        private void btnClick_Click(object sender, EventArgs e)
        {
            if (txtStartTime.Text == "" || txtEndTime.Text == "")
            {
                MessageBox.Show("Please enter trim lengths.");
                return;
            }

            pbVideo.Value = 0;
            pbEncoding.Value = 0;
            var url = txtURL.Text;
            string EnteredResolution = cbResolution.SelectedItem.ToString();
            //var startTime = 0;
            //var endTime = 0;
            int resolution = 360;

            if (EnteredResolution.Equals("240p"))
                resolution = 240;
            else if (EnteredResolution.Equals("480p"))
                resolution = 480;

            string videoDirectory = @"""/Videos";
            Directory.CreateDirectory(videoDirectory);

            int startTime = Convert.ToInt32(txtStartTime.Text.ToString());
            int endTime = Convert.ToInt32(txtEndTime.Text.ToString());
            if (!url.ToLower().Contains("youtube"))
            {
                MessageBox.Show(@"I only eat YouTube videos, sorry.");
                return;
            }

            IEnumerable<VideoInfo> videoInfos = DownloadUrlResolver.GetDownloadUrls(url);
            VideoInfo video = videoInfos.First(info => info.VideoType == VideoType.Mp4 && info.Resolution == resolution);
            if (video.RequiresDecryption)
            {
                DownloadUrlResolver.DecryptDownloadUrl(video);
            }
            var videoDownloader = new VideoDownloader(video, Path.Combine(videoDirectory, video.Title + video.VideoExtension));
            videoDownloader.DownloadProgressChanged += (sender2, args) => SetControlPropertyValue(pbVideo, "value", args.ProgressPercentage); ;
            videoDownloader.Execute();

            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            while (startTime > 60)
            {
                minutes++;
                startTime -= 60;
            }

            while (minutes > 60)
            {
                hours++;
                minutes -= 60;
            }
            seconds = startTime;
            string _hours = "";
            string _minutes = ""; ;
            string _seconds = ""; ;

            if (seconds < 10)
            {
                _seconds = "0" + seconds;
            }
            if (minutes < 10)
            {
                _minutes = "0" + minutes;
            }
            if (minutes < 10)
            {
                _hours = "0" + minutes;
            }

            string _StartTime = _hours + ":" + _minutes + ":" + _seconds + ".0";
            //_StartTime = "00:00:02.0";
            int TrimLength = (endTime - startTime);
            string ext = video.VideoExtension;
            ext += "\"";

            var process = Process.Start(@"ffmpeg\bin\ffmpeg.exe", string.Format("-ss {0} -t {1} -i {2} {3}", _StartTime, TrimLength, videoDirectory + video.Title + ext, videoDirectory + video.Title + "_Cropped" + ext));
            process.WaitForExit();
            process = Process.Start(@"ffmpeg\bin\ffmpeg.exe", string.Format("-i {0} -vcodec mpeg4 -acodec ac3 -b 2048k {1}", videoDirectory + video.Title + "_Cropped" + ext, videoDirectory + video.Title + "_Cropped_Converted.avi"));
            process.WaitForExit();
            string folder = @"\""/Images/";

            if(!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string newVideoExtention = ".avi\"";
            //ffmpeg -i C:\Users\areser\Desktop\small_Cropped_Converted.avi -q:v 2 -vf fps=30 C:\Images\out%4d.jpg
            process = Process.Start(@"ffmpeg\bin\ffmpeg.exe", string.Format("-i {0} -vf scale=320:-1:flags=lanczos,fps=15 {1}out%4d.png", videoDirectory + video.Title + "_Cropped_Converted" + newVideoExtention, folder));
            process.WaitForExit();

            process = Process.Start(@"ImageMagick\convert.exe", string.Format("-loop 0 {0}out*.png {0}output.gif", folder));
            process.WaitForExit();
            process = Process.Start(@"ImageMagick\convert.exe", String.Format("-layers Optimize {0}output.gif {0}output_optimized.gif", folder));
            process.WaitForExit();

            saveFileDialog1.Filter = "Gif files (*.gif)|*.gif|All files (*.*)|*.*";

            string outputFilePath = folder + video.Title + ".gif";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.Copy(outputFilePath, saveFileDialog1.FileName);
                File.Delete(outputFilePath);
            }

            string gifPath = 
            Conversion(folder, video.Title);

            webBrowser1.DocumentText = string.Format(
             "<html><head></head><body><img style='width:100%; height:100%;' src='{0}'</body></html>", saveFileDialog1.FileName);
        }

        private void videoComplete(object sender, DownloadDataCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }
        
        private string Conversion(string path, string VideoFileName)
        {
            List<string> FileNames = new List<string>();

            DirectoryInfo di = new DirectoryInfo(path);
            foreach (var fileName in di.GetFiles())
            {
                FileNames.Add(fileName.ToString());
            }
            string outputFilePath = path + VideoFileName + ".gif";
            AnimatedGifEncoder e = new AnimatedGifEncoder();
                e.Start(outputFilePath);
                e.SetFrameRate(15);
                e.SetQuality(1);
                //-1:no repeat,0:always repeat
                e.SetRepeat(0);
                for (int i = 0, count = FileNames.Count; i < count; i++)
                {
                    e.AddFrame(Image.FromFile(path + FileNames[i]));
                    pbEncoding.Value = Convert.ToInt32(100 * (double)i / (double)count);
                }
                pbEncoding.Value = 100;
                e.Finish();


            return saveFileDialog1.FileName;
        }

        delegate void SetControlValueCallback(Control oControl, string propName, object propValue);
        private void SetControlPropertyValue(Control oControl, string propName, object propValue)
        {
            if (oControl.InvokeRequired)
            {
                SetControlValueCallback d = new SetControlValueCallback(SetControlPropertyValue);
                oControl.Invoke(d, new object[] { oControl, propName, propValue });
            }
            else
            {
                Type t = oControl.GetType();
                PropertyInfo[] props = t.GetProperties();
                foreach (PropertyInfo p in props)
                {
                    if (p.Name.ToUpper() == propName.ToUpper())
                    {
                        p.SetValue(oControl, propValue, null);
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cbResolution.Items.Add("240p");
            cbResolution.Items.Add("360p");
            cbResolution.Items.Add("480p");
            cbResolution.SelectedIndex = 1;

            if (Directory.Exists(@"C:/Images/"))
            {
                DirectoryInfo di = new DirectoryInfo(@"C:/Images/");
                foreach (var fileName in di.GetFiles())
                {
                    File.Delete(fileName.FullName);
                } 
            }
        }

        private void txtStartTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (!char.IsDigit(e.KeyChar)) && (!char.IsControl(e.KeyChar));
        }
        private void txtEndTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = (!char.IsDigit(e.KeyChar)) && (!char.IsControl(e.KeyChar));
        }
    }
}
