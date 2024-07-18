using System;
using System.Collections.Generic;
using System.Windows;

namespace CherryTale_AssetDecDL
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string Root = Environment.CurrentDirectory;
        public static string Respath = String.Empty;
        public static int TotalCount = 0;
        public static int glocount = 0;
        // ping ohgf.uquqp.com => 107.155.58.212
        public static string ServerURL = "https://jifd.yayyf.com/patch/files/";
        public static List<string> log = new List<string>();
    }
}
