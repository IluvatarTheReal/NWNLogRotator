﻿/*  
    *  AUTHOR: notsigma
    *  DATE: 05/23/2020
    *  LICENSE: MIT
*/

using NWNLogRotator.Classes;
using NWNLogRotator.Components;
using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace NWNLogRotator
{
    public partial class MainWindow : Window
    {
        FileHandler instance = new FileHandler();
        Settings _settings;
        System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
        Notification notification = new Notification();
        private int ClientLauncherState = 0;
        private string ProcessName = "nwmain";

        public MainWindow()
        {
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)).Length > 1)
            {
                MessageBox.Show("There is already an instance of NWNLogRotator running!");
                Process.GetCurrentProcess().Kill();
            }
            InitializeComponent();
            SetupApplication();
        }

        private void SetupApplication()
        {
            LoadSettings_Handler();
            IterateNWN_Watcher(false);
        }

        private Settings Settings_Get()
        {
            _settings = instance.InitSettingsIni();
            return _settings;
        }

        private SolidColorBrush Color_Get(string UseTheme, bool OnColor)
        {
            if(OnColor == true)
            {
                if (_settings.UseTheme == "light")
                {
                    return new SolidColorBrush(Colors.DarkGreen);
                }
                else if (_settings.UseTheme == "dark")
                {
                    return new SolidColorBrush(Colors.LawnGreen);
                }
            }
            else
            {
                if (_settings.UseTheme == "light")
                {
                    return new SolidColorBrush(Colors.DarkRed);
                }
                else if (_settings.UseTheme == "dark")
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        private async void IterateNWN_Watcher(bool PreviousStatus)
        {
            _settings = CurrentSettings_Get();
            var OnColor = Color_Get(_settings.UseTheme, true);
            var OffColor = Color_Get(_settings.UseTheme, false);
            int IterateDelay = 5000;
            if(_settings.ServerMode == true)
                ProcessName = "nwserver";
            var Status = NWNProcessStatus_Get(ProcessName);

            if (Status > 0)
            {
                NWNStatusTextBlock.Text = ProcessName + " is active!";
                NWNStatusTextBlock.Foreground = OnColor;

                if (Status == 1)
                {
                    await Task.Delay(IterateDelay);
                    IterateNWN_Watcher(true);
                }
            }
            else
            {
                NWNStatusTextBlock.Text = ProcessName + " not found!";
                NWNStatusTextBlock.Foreground = OffColor;
                if (PreviousStatus == true)
                {
                    if (NWNLog_Save(_settings) == true)
                        UpdateResultsPane(1);
                }
                await Task.Delay(IterateDelay);
                ClientLauncherState = 0;
                IterateNWN_Watcher(false);
            }
        }

        private int NWNProcessStatus_Get(string ProcessName)
        {
            if(ClientLauncherState == 1)
            {
                return 2;
            } 
            else if (ClientLauncherState == 2)
            {
                return 0;
            }
            else
            {
                Process[] processlist = Process.GetProcesses();

                foreach (Process theProcess in processlist)
                {
                    if (theProcess.ProcessName.IndexOf(ProcessName) != -1)
                    {
                        return 1;
                    }
                }
                return 0;
            }      
        }

        private Settings CurrentSettings_Get()
        {
            string OutputDirectory = OutputDirectoryTextBox.Text;
            string PathToLog = PathToLogTextBox.Text;
            int MinimumRowsToInteger = int.Parse(MinimumRowsCountSlider.Value.ToString());
            string ServerName = _settings.ServerName;
            string ServerNameColor = _settings.ServerNameColor;
            bool EventText = EventTextCheckBox.IsChecked.GetValueOrDefault();
            bool CombatText = CombatTextCheckBox.IsChecked.GetValueOrDefault();
            string UseTheme = _settings.UseTheme;
            bool Silent = SilentCheckBox.IsChecked.GetValueOrDefault();
            bool Tray = TrayCheckBox.IsChecked.GetValueOrDefault();
            bool SaveBackup = SaveBackupCheckBox.IsChecked.GetValueOrDefault();
            bool Notifications = NotificationsCheckBox.IsChecked.GetValueOrDefault();
            string CustomEmotes = "";
            if (CustomEmotesCheckBox.IsChecked == true && CustomEmotesTextBox.Text != "")
            {
                CustomEmotes = CustomEmotesTextBox.Text;
            }
            string FilterLines = "";
            if (FilterLinesCheckBox.IsChecked == true && FilterLinesTextBox.Text != "")
            {
                FilterLines = FilterLinesTextBox.Text;
            }
            string PathToClient = _settings.PathToClient;
            bool RunClientOnLaunch = _settings.RunClientOnLaunch;
            bool CloseOnLogGenerated = _settings.CloseOnLogGenerated;
            string ServerAddress = _settings.ServerAddress;
            string ServerPassword = _settings.ServerPassword;
            bool DM = _settings.DM;
            bool ServerMode = ServerModeRadioButton.IsChecked.GetValueOrDefault();

            _settings = new Settings(OutputDirectory,
                                              PathToLog,
                                              MinimumRowsToInteger,
                                              ServerName,
                                              ServerNameColor,
                                              EventText,
                                              CombatText,
                                              UseTheme,
                                              Silent,
                                              Tray,
                                              SaveBackup,
                                              Notifications,
                                              CustomEmotes,
                                              FilterLines,
                                              PathToClient,
                                              RunClientOnLaunch,
                                              CloseOnLogGenerated,
                                              ServerAddress,
                                              ServerPassword,
                                              DM,
                                              ServerMode
                                            );

            return _settings;
        }

        private void SaveToggle_Event()
        {
            if (StatusBarProgressBar.Visibility == Visibility.Collapsed)
            {
                StatusBarProgressBar.Visibility = Visibility.Visible;
                UpdateResultsPane(4);
            }
            else
            {
                StatusBarProgressBar.Visibility = Visibility.Collapsed;
                UpdateResultsPane(-1);
            }
        }

        private void Tray_Set(bool doMinimize)
        {
            if (_settings.Tray == true)
            {
                if (doMinimize == true)
                {
                    WindowState = WindowState.Minimized;
                    this.Hide();
                }
            }
            else
            {
                ni.Visible = false;
            }
        }

        public async void UpdateResultsPane(int result)
        {
            _settings = CurrentSettings_Get();
            var OnColor = Color_Get(_settings.UseTheme, true);
            var OffColor = Color_Get(_settings.UseTheme, false);

            switch (result)
            {
                case -1:
                    EventStatusTextBlock.Text = "";
                    EventStatusTextBlock.Foreground = OnColor;
                    break;
                case 1:
                    EventStatusTextBlock.Text = "Log Saved Successfully!";
                    EventStatusTextBlock.Foreground = OnColor;
                    await Task.Delay(2000);
                    EventStatusTextBlock.Text = "";
                    break;
                case 2:
                    EventStatusTextBlock.Text = "Settings Saved Successfully!";
                    EventStatusTextBlock.Foreground = OnColor;
                    await Task.Delay(1000);
                    EventStatusTextBlock.Text = "";
                    break;
                case 3:
                    EventStatusTextBlock.Text = "Settings Loaded Successfully!";
                    EventStatusTextBlock.Foreground = OnColor;
                    await Task.Delay(2000);
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    string version = fvi.FileVersion;
                    version = "v" + version.Substring(2, version.Length - 2);
                    EventStatusTextBlock.Text = "Version " + version;
                    EventStatusTextBlock.Foreground = OnColor;
                    await Task.Delay(1000);
                    EventStatusTextBlock.Text = "";
                    break;
                case 4:
                    EventStatusTextBlock.Text = "Loading..";
                    EventStatusTextBlock.Foreground = OnColor;
                    break;
            }
        }

        private void LoadSettings_Handler()
        {
            Settings_Get();

            OutputDirectoryTextBox.Text = _settings.OutputDirectory;
            PathToLogTextBox.Text = _settings.PathToLog;
            MinimumRowsCountSlider.Value = _settings.MinimumRowsCount;
            EventTextCheckBox.IsChecked = _settings.EventText;
            CombatTextCheckBox.IsChecked = _settings.CombatText;
            SilentCheckBox.IsChecked = _settings.Silent;
            TrayCheckBox.IsChecked = _settings.Tray;
            SaveBackupCheckBox.IsChecked = _settings.SaveBackup;
            NotificationsCheckBox.IsChecked = _settings.Notifications;
            if (_settings.CustomEmotes != "")
            {
                CustomEmotesCheckBox.IsChecked = true;
                CustomEmotesTextBox.Text = _settings.CustomEmotes;
            }
            if (_settings.FilterLines != "")
            {
                FilterLinesCheckBox.IsChecked = true;
                FilterLinesTextBox.Text = _settings.FilterLines;
            }
            if(_settings.ServerMode == false)
            {
                ClientModeRadioButton.IsChecked = true;
            }
            else
            {
                ServerModeRadioButton.IsChecked = true;
            }
            
            if (_settings.UseTheme == "light")
            {
                ActivateLightTheme();
            }
            else if (_settings.UseTheme == "dark")
            {
                ActivateDarkTheme();
            }

            UpdateResultsPane(3);

            LoadTray_Handler();

            if (_settings.RunClientOnLaunch)
                LaunchClient();
        }

        private void ActivateDarkTheme()
        {
            // purple => black => purple 
            LinearGradientBrush myBrush = new LinearGradientBrush();
            myBrush.GradientStops.Add(new GradientStop(Colors.Purple, 0.0));
            myBrush.GradientStops.Add(new GradientStop(Colors.Black, 0.5));
            myBrush.GradientStops.Add(new GradientStop(Colors.Purple, 1.0));
            Grid.Background = myBrush;

            RunOnceButton.Background = Brushes.Black;
            RunOnceButton.Foreground = new SolidColorBrush(Colors.White);
            TrayCheckBox.Foreground = new SolidColorBrush(Colors.White);
            SilentCheckBox.Foreground = new SolidColorBrush(Colors.White);
            SaveSettingsButton.Background = Brushes.Black;
            SaveSettingsButton.Foreground = new SolidColorBrush(Colors.White);
            LaunchClientButton.Background = Brushes.Black;
            LaunchClientButton.Foreground = new SolidColorBrush(Colors.White);
            LauncherConfigurationButton.Background = Brushes.Black;
            LauncherConfigurationButton.Foreground = new SolidColorBrush(Colors.White);
            ServerConfigurationButton.Background = Brushes.Black;
            ServerConfigurationButton.Foreground = new SolidColorBrush(Colors.White);
            OutputDirectoryTextBox.Background = Brushes.Black;
            PathToLogTextBox.Background = Brushes.Black;
            CustomEmotesTextBox.Background = Brushes.Black;
            FilterLinesTextBox.Background = Brushes.Black;
            MainStatusBar.Background = Brushes.Black;
            OutputDirectoryTextBox.Foreground = new SolidColorBrush(Colors.White);
            PathToLogTextBox.Foreground = new SolidColorBrush(Colors.White);
            SettingsTextBlock.Text = "Dark Mode";
            SettingsTextBlock.Foreground = new SolidColorBrush(Colors.White);
            OutputDirectoryLabel.Foreground = new SolidColorBrush(Colors.White);
            PathToLogLabel.Foreground = new SolidColorBrush(Colors.White);
            CustomEmotesTextBox.Foreground = new SolidColorBrush(Colors.White);
            FilterLinesTextBox.Foreground = new SolidColorBrush(Colors.White);
            EventTextCheckBox.Foreground = new SolidColorBrush(Colors.White);
            CombatTextCheckBox.Foreground = new SolidColorBrush(Colors.White);
            MinimumRowsLabel.Foreground = new SolidColorBrush(Colors.White);
            MinimumRowsCountTextBlock.Foreground = new SolidColorBrush(Colors.White);
            SaveBackupCheckBox.Foreground = new SolidColorBrush(Colors.White);
            NotificationsCheckBox.Foreground = new SolidColorBrush(Colors.White);
            CustomEmotesCheckBox.Foreground = new SolidColorBrush(Colors.White);
            FilterLinesCheckBox.Foreground = new SolidColorBrush(Colors.White);
            ClientModeRadioButton.Foreground = new SolidColorBrush(Colors.White);
            ServerModeRadioButton.Foreground = new SolidColorBrush(Colors.White);
            HintLabel.Foreground = new SolidColorBrush(Colors.White);
            
            _settings.UseTheme = "dark";
        }

        private void ActivateLightTheme()
        {
            Grid.Background = Brushes.White;

            RunOnceButton.Background = Brushes.White;
            RunOnceButton.Foreground = new SolidColorBrush(Colors.Black);
            TrayCheckBox.Foreground = new SolidColorBrush(Colors.Black);
            SilentCheckBox.Foreground = new SolidColorBrush(Colors.Black);
            SaveSettingsButton.Background = Brushes.White;
            SaveSettingsButton.Foreground = new SolidColorBrush(Colors.Black);
            LaunchClientButton.Background = Brushes.White;
            LaunchClientButton.Foreground = new SolidColorBrush(Colors.Black);
            ServerConfigurationButton.Background = Brushes.White;
            ServerConfigurationButton.Foreground = new SolidColorBrush(Colors.Black);
            LauncherConfigurationButton.Background = Brushes.White;
            LauncherConfigurationButton.Foreground = new SolidColorBrush(Colors.Black);
            OutputDirectoryTextBox.Background = Brushes.White;
            PathToLogTextBox.Background = Brushes.White;
            CustomEmotesTextBox.Background = Brushes.White;
            FilterLinesTextBox.Background = Brushes.White;
            MainStatusBar.Background = Brushes.White;
            OutputDirectoryTextBox.Foreground = new SolidColorBrush(Colors.Black);
            PathToLogTextBox.Foreground = new SolidColorBrush(Colors.Black);
            SettingsTextBlock.Text = "Light Mode";
            SettingsTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            OutputDirectoryLabel.Foreground = new SolidColorBrush(Colors.Black);
            PathToLogLabel.Foreground = new SolidColorBrush(Colors.Black);
            CustomEmotesTextBox.Foreground = new SolidColorBrush(Colors.Black);
            FilterLinesTextBox.Foreground = new SolidColorBrush(Colors.Black);
            EventTextCheckBox.Foreground = new SolidColorBrush(Colors.Black);
            CombatTextCheckBox.Foreground = new SolidColorBrush(Colors.Black);
            MinimumRowsLabel.Foreground = new SolidColorBrush(Colors.Black);
            MinimumRowsCountTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            SaveBackupCheckBox.Foreground = new SolidColorBrush(Colors.Black);
            NotificationsCheckBox.Foreground = new SolidColorBrush(Colors.Black);
            CustomEmotesCheckBox.Foreground = new SolidColorBrush(Colors.Black);
            FilterLinesCheckBox.Foreground = new SolidColorBrush(Colors.Black);
            ClientModeRadioButton.Foreground = new SolidColorBrush(Colors.Black);
            ServerModeRadioButton.Foreground = new SolidColorBrush(Colors.Black);
            HintLabel.Foreground = new SolidColorBrush(Colors.Black);

            _settings.UseTheme = "light";
        }

        private bool NWNLog_Save(Settings _settings)
        {
            FileHandler instance = new FileHandler();
            string _filepathandname = instance.ReadNWNLogAndInvokeParser(_settings);

            if (_settings.CloseOnLogGenerated == true)
            {
                Process.GetCurrentProcess().Kill();
            }

            if (_filepathandname != "")
            {
                if(_settings.Notifications == true)
                {
                    notification.ShowNotification("Log file generated successfully!");
                }
                   
                if (_settings.Silent == false)
                {
                    MessageBoxResult _messageBoxResult = MessageBox.Show("The log file has been generated successfully. Would you like to open the log file now?",
                            "Success!",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                    if (_messageBoxResult == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(_filepathandname);

                    return true;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private async void LaunchClient()
        {
            _settings = CurrentSettings_Get();
            var theLaunchPath = "";
            try
            {
                theLaunchPath = Path.GetDirectoryName(_settings.PathToClient);
            }
            catch
            {
                theLaunchPath = "";
            }
            var theLaunchParameters = "";

            if (theLaunchPath != "")
            {
                if (_settings.DM == true)
                {
                    theLaunchParameters += " -dmc";
                }
                if (_settings.ServerAddress != "")
                {
                    theLaunchParameters += " +connect " + _settings.ServerAddress;
                    if (_settings.ServerPassword != "")
                    {
                        theLaunchParameters += " +password " + _settings.ServerPassword;
                    }
                }
                if (_settings.PathToClient.ToLower().Contains("steam.exe"))
                {
                    theLaunchParameters = " -gameidlaunch 704450" + theLaunchParameters;
                }
            }
            if(theLaunchPath != "" && File.Exists(_settings.PathToClient))
            {
                try
                {
                    var p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = _settings.PathToClient;
                    p.StartInfo.Arguments = theLaunchParameters;
                    p.StartInfo.WorkingDirectory = theLaunchPath;
                    p.StartInfo.RedirectStandardOutput = false;
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.CreateNoWindow = true;
                    if (Path.GetFileName(_settings.PathToClient) == ProcessName + ".exe")
                    {
                        ClientLauncherState = 1;
                        IterateNWN_Watcher(false);
                    }
                    await Task.Run(() =>
                    {
                        p.Start();
                        p.WaitForExit();
                    });
                    if( Path.GetFileName(_settings.PathToClient) == ProcessName + ".exe")
                    {
                        ClientLauncherState = 2;
                        IterateNWN_Watcher(true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxResult _messageBoxResult = MessageBox.Show("An issue occured when trying to open the client: " + ex.Message,
                            "Client Not Found!",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                }
            } 
            else
            {
                MessageBoxResult _messageBoxResult = MessageBox.Show("Please configure the launch options, and verify the client path is correct prior to launching!",
                            "Configure Launch Options",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
            }
        }

        private void LoadTray_Handler()
        {
            ni.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };

            Tray_Set(true);
        }

        private void InvertColorScheme(object sender, MouseButtonEventArgs e)
        {
            if (_settings.UseTheme == "light")
            {
                ActivateDarkTheme();
            }
            else
            {
                ActivateLightTheme();
            }
        }

        private void SaveSettings(Settings _settings)
        {
            FileHandler instance = new FileHandler();
            instance.SaveSettingsIni(_settings);

            UpdateResultsPane(2);
        }

        private void SettingsTextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Hand;
        }

        private void SettingsTextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
        }

        private void MinimumRowsCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MinimumRowsCountTextBlock != null)
                MinimumRowsCountTextBlock.Text = e.NewValue.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _settings = CurrentSettings_Get();
            SaveSettings(_settings);
        }

        private void CustomEmotesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CustomEmotesTextBox.Visibility = Visibility.Visible;
        }

        private void CustomEmotesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CustomEmotesTextBox.Text = "";
            CustomEmotesTextBox.Visibility = Visibility.Collapsed;
        }

        private void FilterLinesCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FilterLinesTextBox.Visibility = Visibility.Visible;
        }

        private void FilterLinesCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FilterLinesTextBox.Text = "";
            FilterLinesTextBox.Visibility = Visibility.Collapsed;
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _settings = CurrentSettings_Get();
            bool SavedLogResult = false;
            SaveToggle_Event();

            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            await Task.Run(() =>
            {
                SavedLogResult = NWNLog_Save(_settings);
            });
       
            SaveToggle_Event();
            SavedResult_Callback(SavedLogResult);
        }

        private void SavedResult_Callback(bool result)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            if (result == true)
                UpdateResultsPane(1);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            _settings.Tray = _settings.Tray ? false : true;
            Tray_Set(false);
        }
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                this.Hide();
                ni.Visible = true;
            }
                
            if (WindowState == System.Windows.WindowState.Normal)
            {
                ni.Visible = false;
            }
                

            base.OnStateChanged(e);
        }
        private void WindowClosed_Event(object sender, CancelEventArgs e)
        {
            ni.Visible = false;
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.SelectedPath = _settings.OutputDirectory;
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                    OutputDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void Image_MouseDown2(object sender, MouseButtonEventArgs e)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.InitialDirectory = _settings.PathToLog;
            fileDialog.Filter = "txt file (*.txt)|*.txt";
            fileDialog.FilterIndex = 1;
            fileDialog.RestoreDirectory = true;
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    PathToLogTextBox.Text = file;
                    PathToLogTextBox.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                    break;
                default:
                    break;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            LaunchClient();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            _settings = CurrentSettings_Get();
            ExportConfiguration ExportConfigurationPopUp = new ExportConfiguration(_settings);
            ExportConfigurationPopUp.Owner = Window.GetWindow(this);
            Grid.Effect = new BlurEffect();
            ExportConfigurationPopUp.ShowDialog();
            Settings __settings = ExportConfigurationPopUp.Settings_Get();
            bool __closed = ExportConfigurationPopUp.Closed_Get();
            if (_settings != __settings && !__closed)
            {
                _settings = __settings;
                SaveSettings(_settings);
            }
            Grid.Effect = null;
        }
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            _settings = CurrentSettings_Get();
            LauncherConfiguration LauncherConfigurationPopUp = new LauncherConfiguration(_settings);
            LauncherConfigurationPopUp.Owner = Window.GetWindow(this);
            Grid.Effect = new BlurEffect();
            LauncherConfigurationPopUp.ShowDialog();
            Settings __settings = LauncherConfigurationPopUp.Settings_Get();
            bool __closed = LauncherConfigurationPopUp.Closed_Get();
            if( _settings != __settings && !__closed )
            {
                _settings = __settings;
                SaveSettings(_settings);
            }
            Grid.Effect = null;
        }
    }
}
