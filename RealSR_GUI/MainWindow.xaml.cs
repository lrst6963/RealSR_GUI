using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Microsoft.Win32;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System.Collections;
using System.Diagnostics;

namespace RealSR_GUI
{
    /// <summary>
    /// 主窗口交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 常量与字段
        public bool NoWindow = false;
        private const string FileFilter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.jfif|所有文件|*.*";
        private const string FileDialogTitle = "选择文件";

        // 主题颜色配置字典（Key: 颜色名称，Value: 颜色值）
        private readonly Dictionary<string, Color> _themeColors = new()
        {
            {"Red", Colors.Red}, {"Pink", Colors.Pink}, {"Purple", Colors.Purple},
            {"DeepPurple", Colors.DeepPink}, {"Indigo", Colors.Indigo}, {"Blue", Colors.Blue},
            {"LightBlue", Colors.LightBlue}, {"Cyan", Colors.Cyan}, {"Teal", Colors.Teal},
            {"Green", Colors.Green}, {"LightGreen", Colors.LightGreen}, {"Lime", Colors.Lime},
            {"Yellow", Colors.Yellow}, {"Orange", Colors.Orange}, {"DeepSkyBlue", Colors.DeepSkyBlue},
            {"Brown", Colors.Brown}, {"Gray", Colors.Gray}, {"SkyBlue", Colors.SkyBlue},
            {"Amber", Color.FromArgb(255, 255, 190, 0)}
        };

        private string _currentImageType = "jpg";  // 当前选择的图片格式
        private string[] _selectedFiles = Array.Empty<string>();  // 选择的文件路径数组
        private bool _hideProcessWindow;  // 是否隐藏处理窗口
        #endregion

        #region 初始化
        public MainWindow()
        {
            InitializeComponent();
            InitializeThemeColors();
        }

        /// <summary>
        /// 窗口加载时初始化组件
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeModules();     // 初始化模型列表
            InitializeImageTypes(); // 初始化图片类型
            ValidateDependencies(); // 验证依赖文件
        }

        /// <summary>
        /// 初始化超分辨率模型列表
        /// </summary>
        private void InitializeModules()
        {
            var modules = new[]
            {
                "realesrgan-x4plus-anime",
                "realesrgan-x4plus",
                "realesr-animevideov3-x2",
                "realesr-animevideov3-x3",
                "realesr-animevideov3-x4"
            };
            ComboBox_Module.ItemsSource = modules;
            ComboBox_Module.SelectedIndex = 0;  // 默认选择第一个
        }

        /// <summary>
        /// 初始化主题颜色下拉框
        /// </summary>
        private void InitializeThemeColors()
        {
            ThemeComboBox.ItemsSource = _themeColors.Keys;
            ThemeComboBox.SelectedIndex = 0;  // 默认选择第一个
        }

        /// <summary>
        /// 初始化输出图片格式选项
        /// </summary>
        private void InitializeImageTypes()
        {
            ComboBox_ImgType.ItemsSource = new[] { "JPG", "PNG", "WEBP" };
            ComboBox_ImgType.SelectedIndex = 0;  // 默认选择JPG
        }

        /// <summary>
        /// 验证必要依赖文件是否存在
        /// </summary>
        private void ValidateDependencies()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            START.IsEnabled = File.Exists(System.IO.Path.Combine(baseDir, "realesrgan-ncnn-vulkan.exe"));
            ComboBox_ImgType.IsEnabled = File.Exists(System.IO.Path.Combine(baseDir, "ffmpeg.exe"));
        }
        #endregion

        #region 文件操作
        /// <summary>
        /// 打开文件选择对话框
        /// </summary>
        private void OpenFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = FileFilter,
                Title = FileDialogTitle,
                RestoreDirectory = true,
                Multiselect = CheckBox_3.IsChecked == true,  // 根据多选复选框决定
                FilterIndex = 1,
                DefaultExt = "jpg"
            };

            if (dialog.ShowDialog() != true) return;

            _selectedFiles = dialog.FileNames;
            UpdateFileDisplay();  // 更新界面文件显示
        }

        /// <summary>
        /// 更新界面文件路径显示
        /// </summary>
        private void UpdateFileDisplay()
        {
            var count = _selectedFiles.Length;
            // 多文件显示数量，单文件显示完整路径
            filedirs.Content = count > 1
                ? $"({count}个文件)"
                : count == 1
                    ? _selectedFiles[0]
                    : string.Empty;

            filedirs_out.Content = count > 1 ? $"({count}个文件)" : string.Empty;
        }
        #endregion

        #region 核心处理逻辑
        /// <summary>
        /// 开始处理按钮点击事件
        /// </summary>
        private async void StartProcessing_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFiles.Length == 0)
            {
                MessageBox.Show("请先选择要处理的文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ToggleControls(false);  // 禁用操作控件

            try
            {
                for (var i = 0; i < _selectedFiles.Length; i++)
                {
                    await ProcessFile(_selectedFiles[i]);  // 处理单个文件
                    UpdateProgress(i + 1);  // 更新进度显示
                }
                ShowCompletionMessage();  // 显示完成提示
            }
            finally
            {
                ToggleControls(true);  // 确保控件状态恢复
            }
        }

        /// <summary>
        /// 处理单个文件
        /// </summary>
        /// <param name="inputPath">输入文件路径</param>
        private async Task ProcessFile(string inputPath)
        {
            // 生成输出路径（暂存为PNG）
            var tempOutput = GetOutputPath(inputPath, "png");

            // 构建realesrgan命令
            var realesrganCmd = $"realesrgan-ncnn-vulkan -i \"{inputPath}\" -o \"{tempOutput}\" -n {ComboBox_Module.Text}";

            // 根据输出格式构建转换命令
            if (_currentImageType != "png")
            {
                var conversionCmd = GetConversionCommand(tempOutput);
                realesrganCmd += $" & {conversionCmd}";
            }

            await ExecuteCommand(realesrganCmd);  // 执行组合命令

            // 如果启用"完成后打开"选项
            if (CheckBox_1.IsChecked == true)
            {
                OpenOutputDirectory(System.IO.Path.GetDirectoryName(tempOutput));
            }
            filedirs_out.Content = tempOutput;
        }

        /// <summary>
        /// 生成格式转换命令
        /// </summary>
        private string GetConversionCommand(string tempPath)
        {
            var finalOutput = System.IO.Path.ChangeExtension(tempPath, _currentImageType);
            return _currentImageType switch
            {
                "jpg" => $"ffmpeg.exe -y -i \"{tempPath}\" -q 1 \"{finalOutput}\" & del \"{tempPath}\"",
                "webp" => $"ffmpeg.exe -i \"{tempPath}\" -vf scale=iw:ih -codec libwebp -lossless 0 -q 100 \"{finalOutput}\" & del \"{tempPath}\"",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 执行命令行指令
        /// </summary>
        public async Task ExecuteCommand(string command)
        {
            bool hideWindow = NoWindow;
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {

                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    CreateNoWindow = hideWindow  // 不创建命令行窗口
                }
            };
            process.Start();
            await process.WaitForExitAsync();  // 异步等待完成
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 切换控件可用状态
        /// </summary>
        private void ToggleControls(bool enable)
        {
            START.IsEnabled = enable;
            Open_Dir.IsEnabled = enable;
            ComboBox_Module.IsEnabled = enable;
            ComboBox_ImgType.IsEnabled = enable;
        }

        /// <summary>
        /// 更新处理进度显示
        /// </summary>
        private void UpdateProgress(int completed)
        {
            filedirs_out.Content = $"({completed}/{_selectedFiles.Length}个文件已完成)";
        }

        /// <summary>
        /// 显示完成提示
        /// </summary>
        private void ShowCompletionMessage()
        {
            MessageBox.Show("所有文件处理完成！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            //filedirs_out.Content = "处理完成";
        }

        /// <summary>
        /// 打开输出目录
        /// </summary>
        private static void OpenOutputDirectory(string directoryPath)
        {
            Process.Start("explorer.exe", $"\"{directoryPath}\"");
        }

        /// <summary>
        /// 生成输出文件路径
        /// </summary>
        private static string GetOutputPath(string inputPath, string extension)
        {
            var fileName = $"{System.IO.Path.GetFileNameWithoutExtension(inputPath)}-ENLARGE-.{extension}";
            return  System.IO.Path.Combine(System.IO.Path.GetDirectoryName(inputPath), fileName);
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 主题颜色选择变更事件
        /// </summary>
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is not string colorName) return;

            var paletteHelper = new PaletteHelper();
            Theme theme = paletteHelper.GetTheme();

            if (_themeColors.TryGetValue(colorName, out var color))
            {
                theme.SetPrimaryColor(color);
                paletteHelper.SetTheme(theme);
            }
        }

        /// <summary>
        /// 图片格式选择变更事件
        /// </summary>
        private void ComboBox_ImgType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentImageType = ComboBox_ImgType.SelectedIndex switch
            {
                0 => "jpg",
                1 => "png",
                2 => "webp",
                _ => "jpg"
            };
        }

        /// <summary>
        /// 路径标签点击复制事件
        /// </summary>
        private void PathLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Label label && !string.IsNullOrEmpty(label.Content?.ToString()))
            {
                Clipboard.SetText(label.Content.ToString());
                MessageBox.Show("路径已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

        /// <summary>
        /// 多选模式复选框点击事件
        /// </summary>
        private void MultiSelectCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_3.IsChecked == true)
            {
                CheckBox_1.IsEnabled = false;  // 禁用"完成后打开"选项
                CheckBox_2.IsChecked = true;   // 自动启用"隐藏窗口"
                _hideProcessWindow = true;
            }
            else
            {
                CheckBox_1.IsEnabled = true;
                CheckBox_2.IsChecked = false;
                _hideProcessWindow = false;
            }
        }
        #endregion
    }
}