using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CherryTale_AssetDecDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 同時下載的線程池上限
        /// </summary>
        int pool = 50;

        private async void btn_download_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = App.Root;
            openFileDialog.Filter = "index.txt|*.txt";
            if (!openFileDialog.ShowDialog() == true)
                return;

            JObject ResList = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
            List<Tuple<string, string>> AssetList = new List<Tuple<string, string>>();

            foreach (JProperty jp in ResList.Properties())
            {
                string url = jp.Name.ToString();
                string hash = JArray.Parse(jp.Value.ToString())[1].ToString();

                AssetList.Add(new Tuple<string, string>(url, hash));
            }

            App.TotalCount = AssetList.Count;

            if (App.TotalCount > 0)
            {
                App.Respath = Path.Combine(App.Root, "Asset");
                if (!Directory.Exists(App.Respath))
                    Directory.CreateDirectory(App.Respath);

                int count = 0;
                List<Task> tasks = new List<Task>();
                foreach (Tuple<string, string> asset in AssetList)
                {
                    string name = asset.Item1;
                    //string url = App.ServerURL + asset.Item1; // old version (unencrypted)
                    string url = $"{App.ServerURL}{asset.Item1}.{asset.Item2}";

                    string path = Path.Combine(App.Respath, name);

                    tasks.Add(DownLoadFile(url, path, cb_isCover.IsChecked == true ? true : false));
                    count++;

                    // 阻塞線程，等待現有工作完成再給新工作
                    if ((count % pool).Equals(0) || App.TotalCount == count)
                    {
                        // await is better than Task.Wait()
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    // 用await將線程讓給UI更新
                    lb_counter.Content = $"進度 : {count} / {App.TotalCount}";
                    await Task.Delay(1);
                }

                if (cb_Debug.IsChecked == true && App.log.Count > 0)
                {
                    using (StreamWriter outputFile = new StreamWriter("404.log", false))
                    {
                        foreach (string s in App.log)
                            outputFile.WriteLine(s);
                    }
                }

                string failmsg = String.Empty;
                if (App.TotalCount - App.glocount > 0)
                    failmsg = $"，{App.TotalCount - App.glocount}個檔案失敗";

                System.Windows.MessageBox.Show($"下載完成，共{App.glocount}個檔案{failmsg}", "Finish");
                lb_counter.Content = String.Empty;
            }
        }

        /// <summary>
        /// 從指定的網址下載檔案
        /// </summary>
        public async Task<Task> DownLoadFile(string downPath, string savePath, bool overWrite)
        {
            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            }
            
            if (File.Exists(savePath) && overWrite == false)
                return Task.FromResult(0);

            App.glocount++;

            using (WebClient wc = new WebClient())
            {
                //Test code
                /*byte[] data = await wc.DownloadDataTaskAsync(downPath);
                if (CheckSign(data))
                    data = ChangeVersion(data);
                    data = ChangeIdx(data);
                */
                try
                {
                    // Don't use DownloadFileTaskAsync, if 404 it will create a empty file, use DownloadDataTaskAsync instead.
                    byte[] data = await wc.DownloadDataTaskAsync(downPath);
                    if (CheckSign(data))
                        data = ChangeIdx(ChangeVersion(data));
                    File.WriteAllBytes(savePath, data);
                    
                }
                catch (Exception ex)
                {
                    App.glocount--;

                    if (cb_Debug.IsChecked == true)
                        App.log.Add(downPath + Environment.NewLine + savePath + Environment.NewLine);

                    // 沒有的資源直接跳過，避免報錯。
                    //System.Windows.MessageBox.Show(ex.Message.ToString() + Environment.NewLine + downPath + Environment.NewLine + savePath);
                }
                
            }
            return Task.FromResult(0);
        }

        public byte[] ChangeVersion(byte[] data)
        {
            // 2018.3.5f1.
            byte[] verion_fake = { 0x32, 0x30, 0x31, 0x38, 0x2E, 0x33, 0x2E, 0x35, 0x66, 0x31, 0x00 };
            // 2020.3.41f1
            byte[] verion = { 0x32, 0x30, 0x32, 0x30, 0x2E, 0x33, 0x2E, 0x34, 0x31, 0x66, 0x31 };

            long data_size = data.Length;
            int datahead_size = 0x300;
            byte[] datahead = new byte[datahead_size];
            Array.Copy(data, datahead, datahead_size);

            ArrayReplaceAll(datahead, verion_fake, verion);

            byte[] newdata = new byte[data_size];
            Array.Copy(datahead, 0, newdata, 0, datahead_size);
            Array.Copy(data, datahead_size, newdata, datahead_size, data_size - datahead_size);

            return newdata;
        }

        public bool CheckSign(byte[] data)
        {
            string signed = "UnityFS";
            long data_size = data.Length;

            if (data_size > signed.Length)
            {
                byte[] tmp = new byte[signed.Length];
                Array.Copy(data, tmp, signed.Length);
                if (Encoding.UTF8.GetString(tmp) == signed)
                    return true;
            }
            return false;
        }

        public void ArrayReplaceAll(byte[] source, byte[] oldBytes, byte[] newBytes)
        {
            for (int i = 0; i < source.Length - oldBytes.Length + 1; i++)
            {
                bool match = true;
                for (int j = 0; j < oldBytes.Length; j++)
                {
                    if (source[i + j] != oldBytes[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    Array.Copy(newBytes, 0, source, i, newBytes.Length);
                }
            }
        }

        public byte[] ChangeIdx(byte[] iFileData)
        {
            int v4 = 0, v8, v11, v12, v19;
            byte v21;
            int[] v7;
            if (iFileData.Length > 0x270C)
                v7 = new int[3] { 0x3FB, 0xD99, 0x197C };
            else
                v7 = new int[3] { 0x3FB, 0xC68, 0xD99 };

            while (true)
            {
                v11 = v7.Length;
                if (v4 >= v11)
                    break;

                // v9 = v7->m_Items; v12 = *(_DWORD *)&v9[4 * v4];
                v12 = v7[v4];
                v8 = iFileData.Length;
                if (v8 > v12)
                {
                    v21 = iFileData[v12];
                    iFileData[v12] = iFileData[v8 - v12];
                    v19 = iFileData.Length - v12;
                    iFileData[v19] = v21;
                }
                v4++;
            }

            return iFileData;
        }
    }
}
