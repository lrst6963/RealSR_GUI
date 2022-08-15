using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System.Collections;

namespace RealSR_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Imgtype = "jpg";
        private const string V = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.jfif|所有文件|*.*";
        private const string T = "选择文件";
        string[] FileArrList = new string[1];
        bool NoWindow = false;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox_Module.Items.Add("realesrgan-x4plus-anime");
            ComboBox_Module.Items.Add("realesrgan-x4plus");
            string[,] Colors = { { "Red", "Pink", "Purple", "DeepPurple", "Indigo", "Blue", "LightBlue", "Cyan", "Teal", "Green", "LightGreen", "Lime", "Yellow", "Orange", "DeepSkyBlue", "Brown", "Grey", "SkyBlue", "Amber" } };
            foreach (string Color in Colors)
            {
                ThemeComboBox.Items.Add(Color);
            }
            ComboBox_ImgType.Items.Add("JPG");
            ComboBox_ImgType.Items.Add("PNG");
            ComboBox_ImgType.Items.Add("WEBP");
            if (File.Exists(Environment.CurrentDirectory + "\\realesrgan-ncnn-vulkan.exe") != true)
            {
                MessageBox.Show("当前缺失RealESRGAN核心程序！", "提示", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                START.IsEnabled = false;
            }
            if (File.Exists(Environment.CurrentDirectory + "\\ffmpeg.exe") != true)
            {
                ComboBox_ImgType.IsEnabled = false;
            }
        }

        private void Open_Dir_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_3.IsChecked == true)
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = V,
                    Title = T,
                    RestoreDirectory = true,
                    Multiselect = true,
                    FilterIndex = 1,
                    DefaultExt = "jpg"
                };
                if (openFileDialog.ShowDialog() == true)
                {

                    FileArrList = openFileDialog.FileNames;
                    filedirs.Content = "(" + FileArrList.Length.ToString() + "个文件)";
                    filedirs_out.Content = "(" + FileArrList.Length.ToString() + "个文件)";
                }
            }
            else
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = V,
                    Title = T,
                    FileName = string.Empty,
                    FilterIndex = 1,
                    Multiselect = false,
                    RestoreDirectory = true,
                    DefaultExt = "jpg"
                };
                if (openFileDialog.ShowDialog() == false)
                {
                    return;
                }
                filedirs.Content = openFileDialog.FileName;
                filedirs_out.Content = string.Empty;
            }
        }

        private void START_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists((string)filedirs.Content) != true)
            {
                if ((string)filedirs.Content != string.Empty)//选择文件不为空时执行
                {
                    foreach (string item in FileArrList)
                    {
                        string filename_NoEx = System.IO.Path.GetFileNameWithoutExtension(item);
                        string filename = System.IO.Path.GetFileNameWithoutExtension(item) + "x4." + "png";
                        string outputfile = System.IO.Path.GetDirectoryName(item) + "\\" + filename;
                        string commd = string.Format(@"realesrgan-ncnn-vulkan -i ""{0}"" -o ""{1}"" -n {2}", item, outputfile, ComboBox_Module.Text);
                        Execute(NoWindow, commd);
                        if (Imgtype == "jpg")//转换JPG
                        {
                            string outputfiles = System.IO.Path.GetDirectoryName(outputfile) + "\\" + filename_NoEx + "x4." + "jpg";
                            string commds = string.Format(@"ffmpeg.exe -y -i ""{0}"" -q 1 ""{1}"" & del ""{0}""", outputfile, outputfiles);
                            Execute(NoWindow, commds);
                        }
                        else if (Imgtype == "webp")//转换WEBP
                        {
                            string outputfiles = System.IO.Path.GetDirectoryName(outputfile) + "\\" + filename_NoEx + "x4." + "webp";
                            string commds = string.Format(@"ffmpeg.exe -i ""{0}""  -vf scale=iw:ih -codec libwebp -lossless 0 -q 100 ""{1}"" & del ""{0}""", outputfile, outputfiles);
                            Execute(NoWindow, commds);
                        }
                        //if (CheckBox_1.IsChecked == true)//完成后打开
                        //{
                        //    Execute(true, string.Format(@"explorer ""{0}""", outputfile));
                        //}
                    }
                    MessageBox.Show("完成!");
                }
            }
            else
            {
                if ((string)filedirs.Content != string.Empty)//选择文件不为空时执行
                {
                    string filename_NoEx = System.IO.Path.GetFileNameWithoutExtension((string)filedirs.Content);
                    string filename = filename_NoEx + "x4." + "png";
                    string outputfile = System.IO.Path.GetDirectoryName((string)filedirs.Content) + "\\" + filename;
                    string commd = string.Format(@"realesrgan-ncnn-vulkan -i ""{0}"" -o ""{1}"" -n {2}", filedirs.Content, outputfile, ComboBox_Module.Text);
                    Execute(NoWindow, commd);
                    filedirs_out.Content = outputfile;
                    if (Imgtype == "jpg")//转换JPG
                    {
                        string outputfiles = System.IO.Path.GetDirectoryName(outputfile) + "\\" + filename_NoEx + "x4." + "jpg";
                        string commds = string.Format(@"ffmpeg.exe -y -i ""{0}"" -q 1 ""{1}"" & del ""{0}""", outputfile, outputfiles);
                        Execute(NoWindow,commds);
                        if (CheckBox_1.IsChecked == true)
                        {
                            Execute(true, string.Format(@"explorer ""{0}""", outputfiles));
                        }
                        filedirs_out.Content = outputfile;
                    }
                    else if (Imgtype == "webp")//转换WEBP
                    {
                        string outputfiles = System.IO.Path.GetDirectoryName(outputfile) + "\\" + filename_NoEx + "x4." + "webp";
                        string commds = string.Format(@"ffmpeg.exe -i ""{0}""  -vf scale=iw:ih -codec libwebp -lossless 0 -q 100 ""{1}"" & del ""{0}""", outputfile, outputfiles);
                        Execute(NoWindow, commds);
                        if (CheckBox_1.IsChecked == true)
                        {
                            Execute(true, string.Format(@"explorer ""{0}""", outputfiles));
                        }
                        filedirs_out.Content = outputfiles;
                    }
                    if (CheckBox_1.IsChecked == true)
                    {
                        if (File.Exists(outputfile) == true)
                        {
                            Execute(true, string.Format(@"explorer ""{0}""", outputfile));
                        }
                    }
                }
            }
        }


        public static void Execute(bool NoWindow, string command)
        {
            System.Diagnostics.Process process = new();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c" + command;
            process.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
            process.StartInfo.CreateNoWindow = NoWindow;//不显示程序窗口
            process.Start();
            process.WaitForExit(); //等待程序执行完退出进程
            process.Close();
        }

        private void filedirs_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (string.Format("{0}", filedirs.Content) != string.Empty)
            {
                Clipboard.SetDataObject(filedirs.Content);
                MessageBox.Show("已复制到剪切板！", "");
            }
        }

        private void filedirs_out_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (string.Format("{0}", filedirs_out.Content) != string.Empty)
            {
                Clipboard.SetDataObject(filedirs_out.Content);
                MessageBox.Show("已复制到剪切板！", "");
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var paletteHelper = new PaletteHelper();
            ITheme theme = paletteHelper.GetTheme();
            switch (ThemeComboBox.SelectedIndex)
            {
                case 0:
                    theme.SetPrimaryColor(Colors.Red);
                    break;
                case 1:
                    theme.SetPrimaryColor(Colors.Pink);
                    break;
                case 2:
                    theme.SetPrimaryColor(Colors.Purple);
                    break;
                case 3:
                    theme.SetPrimaryColor(Colors.DeepPink);
                    break;
                case 4:
                    theme.SetPrimaryColor(Colors.Indigo);
                    break;
                case 5:
                    theme.SetPrimaryColor(Colors.Blue);
                    break;
                case 6:
                    theme.SetPrimaryColor(Colors.LightBlue);
                    break;
                case 7:
                    theme.SetPrimaryColor(Colors.Cyan);
                    break;
                case 8:
                    theme.SetPrimaryColor(Colors.Teal);
                    break;
                case 9:
                    theme.SetPrimaryColor(Colors.Green);
                    break;
                case 10:
                    theme.SetPrimaryColor(Colors.LightGreen);
                    break;
                case 11:
                    theme.SetPrimaryColor(Colors.Lime);
                    break;
                case 12:
                    theme.SetPrimaryColor(Colors.Yellow);
                    break;
                case 13:
                    theme.SetPrimaryColor(Colors.Orange);
                    break;
                case 14:
                    theme.SetPrimaryColor(Colors.DeepSkyBlue);
                    break;
                case 15:
                    theme.SetPrimaryColor(Colors.Brown);
                    break;
                case 16:
                    theme.SetPrimaryColor(Colors.Gray);
                    break;
                case 17:
                    theme.SetPrimaryColor(Colors.SkyBlue);
                    break;
                case 18:
                    theme.SetPrimaryColor(Color.FromArgb(255, 255, 190, 0));
                    break;
                default:
                    break;
            }
            paletteHelper.SetTheme(theme);
        }

        private void ComboBox_ImgType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox_ImgType.SelectedIndex == 0)
            {
                Imgtype = "jpg";
            }
            else if (ComboBox_ImgType.SelectedIndex == 1)
            {
                Imgtype = "png";
            }
            else if (ComboBox_ImgType.SelectedIndex == 2)
            {
                Imgtype = "webp";
            }
        }

        private void CheckBox_3_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_3.IsChecked == true)
            {
                CheckBox_1.IsEnabled = false;
                //CheckBox_2.IsEnabled = false;
            }
            else
            {
                CheckBox_1.IsEnabled = true;
                //CheckBox_2.IsEnabled = true;
            }
        }

        private void CheckBox_2_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_2.IsChecked == true)
            {
                NoWindow = true;
            }
            else
            {
                NoWindow = false;
            }
        }
    }

}
