using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Ionic.AppUpdater.Wpf;
using MahApps.Metro.Controls;
using PandaVideo.Properties;
using PandaVideoMixer;
using Cursors = System.Windows.Input.Cursors;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace PandaVideo
{
    /// <summary>
    ///   Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly ObservableCollection<PandaVideoConv> _convList = new ObservableCollection<PandaVideoConv>();

        private PandaVideoConv _convSelection;
        private readonly Log logging;
        private Int32 _numFailures = 0;
        private readonly BackgroundWorker _backgroundWorker1 = new BackgroundWorker();


        private Updater _updater;

        private const string ManifestUrl = "http://andytather.co.uk/Panda/Files/PVC/Manifest.xml";

        private const string InfoUrl = "http://andytather.co.uk/Panda/VideoConverter.aspx";

        private const string Description = "Pandasoft Video Converter updater ";

        // Obtain the publicKey XML from the ManifestTool that ships with Ionic AppUpdater.
        // The following string is Ionic's public key.  You will need a different one.
        // Consult the Readme for more information. 
        private const string PublicKeyXml = "<RSAKeyValue><Modulus>sZqhvF0KX6m4LkSDe5Le6FGU0Dug6N4smJ8LEjVsdWXxmnN3EsAem5sbNUnS0EJ7fTbfyisBy/9L+9y5t/5inIeo1dLVKjCah0XBAZfImG4wneQ8QCje0zCNbhc3kPin9HSYnJUzpzRb0pnehKYPyBvZ9wEzwL64fkkX97OpaNM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";


        public MainWindow()
        {
            InitializeComponent();

            if (bool.Parse(Settings.Default.AutoUpdateCheck))
                UpdaterCheck();

            logging = new Log(); // conv.GetLog;

            logging.CollectionChanged += new NotifyCollectionChangedEventHandler(LoggingCollectionChanged);
            _convList.CollectionChanged += new NotifyCollectionChangedEventHandler(ConvListCollectionChanged);

            _backgroundWorker1.DoWork +=
                new DoWorkEventHandler(BackgroundWorker1DoWork);
            _backgroundWorker1.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(
                    BackgroundWorker1RunWorkerCompleted);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Settings.Default.UpdateRequired)
                {
                    Settings.Default.Upgrade();
                    Settings.Default.UpdateRequired = false;
                }


                // Turn convert off until we have some analysis
                buttonConvert.IsEnabled = false;
                tabItem1.IsEnabled = false;
                buttonClear.IsEnabled = false;
                buttonDel.IsEnabled = false;

                listViewConversions.ItemsSource = _convList;
                listViewLog.ItemsSource = logging;
                progressBar1.Maximum = 100;
                progressBar1.Minimum = 0;

                textBoxOutputFilePath.Text = Settings.Default.OutputFolder;
                textBoxWorkingFolder.Text = Settings.Default.WorkingFolder;

                if (String.IsNullOrEmpty(textBoxWorkingFolder.Text))
                    textBoxWorkingFolder.Text = Path.GetTempPath();

                // setup output device
                comboBoxDevice.ItemsSource = PandaVideoConv.OutputDeviceList;
                var selected = PandaVideoConv.GetOutputDeviceByName(Settings.Default.OutputDeviceName);
                if (selected != null)
                    comboBoxDevice.SelectedItem = selected;
                else
                    comboBoxDevice.SelectedIndex = 0;

                comboBoxPrefAudioLang.ItemsSource = PandaVideoConv.LanguageList;
                var prefAudioSelected = PandaVideoConv.GetAudioLanguageByDisplayName(Settings.Default.PrefAudioLanguage);
                if (prefAudioSelected != null)
                    comboBoxPrefAudioLang.SelectedItem = prefAudioSelected;
                else
                    comboBoxPrefAudioLang.SelectedIndex = 2; //eng


                checkBoxEncodeSubs.IsChecked = bool.Parse(Settings.Default.EncodeSubtitles);
                checkBoxAutoCheckUpdate.IsChecked = bool.Parse(Settings.Default.AutoUpdateCheck);
                checkBoxUseHEVC.IsChecked = bool.Parse(Settings.Default.UseHEVC);

                // Get assembly details to extract built version numbers etc
                Assembly oAssembly = Assembly.GetExecutingAssembly();
                FileVersionInfo oFileVersionInfo = FileVersionInfo.GetVersionInfo(oAssembly.Location);
                string fileVersion = oFileVersionInfo.FileVersion;
                string titleversion = oFileVersionInfo.FileMajorPart + "." + oFileVersionInfo.FileMinorPart;
                // update dlg titles
                labelProductVersion.Content = fileVersion;
                Title = Title + titleversion;

                // Put up some O/S details
                labelOS.Content = String.Format("Running in {0} mode on an {1} O/S", Environment.Is64BitProcess ? "64bit" : "32bit", Environment.Is64BitOperatingSystem ? "64bit" : "32bit");


                // Give user a hint
                logging.Add(new LogItem(DateTime.Now,
                                        "Select required output device and settings then click the batch tab to add files.",
                                        null));
            }
            catch (Exception)
            {
                // Problem with start-up - allow user to recofigure?
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Settings.Default.OutputFolder = textBoxOutputFilePath.Text;
            Settings.Default.OutputDeviceName = (comboBoxDevice.SelectedItem as IOutputDevice).Name;
            Settings.Default.WorkingFolder = textBoxWorkingFolder.Text;
            Settings.Default.PrefAudioLanguage = (comboBoxPrefAudioLang.SelectedItem as AudioLanguage).DisplayName;
            Settings.Default.EncodeSubtitles = checkBoxEncodeSubs.IsChecked.ToString();
            Settings.Default.AutoUpdateCheck = checkBoxAutoCheckUpdate.IsChecked.ToString();
            Settings.Default.UseHEVC = checkBoxUseHEVC.IsChecked.ToString();
            Settings.Default.Save();
        }

        private void ConvListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_convList.Count > 0)
            {
                buttonClear.IsEnabled = true;
                buttonDel.IsEnabled = true;
                buttonConvert.IsEnabled = true;
            }
            else
            {
                buttonClear.IsEnabled = false;
                buttonDel.IsEnabled = false;
                buttonConvert.IsEnabled = false;
            }
        }

        public delegate void StatusDelegate(ProgressEventArgs arg);

        private void ConvStatusChanged(object sender, ProgressEventArgs e)
        {
            progressBar1.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new StatusDelegate(UpdateProgress), e);
        }

        public void UpdateProgress(ProgressEventArgs progress)
        {
            if (progress.Percent > progressBar1.Maximum)
                progressBar1.Value = progressBar1.Maximum;
            else if (progress.Percent < progressBar1.Minimum)
                progressBar1.Value = progressBar1.Minimum;
            else
                progressBar1.Value = progress.Percent;

            // If we can't show progress - let it do it itself
            progressBar1.IsIndeterminate = progress.Indeterminate;

            if (!String.IsNullOrEmpty(progress.Message))
                logging.Add(new LogItem(DateTime.Now, progress.Message, progress.Data));
        }

        private void LoggingCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (listViewLog.Items.Count > 0)
            {
                listViewLog.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                   new SetSelectedLogItemDelegate(SetSelectedLogItem));
            }
        }

        public delegate void SetSelectedLogItemDelegate();

        public void SetSelectedLogItem()
        {
            listViewLog.SelectedIndex = listViewLog.Items.Count - 1;
            listViewLog.ScrollIntoView(listViewLog.SelectedItem);
            listViewLog.Focus();
        }


        private void UpdatePanel(PandaVideoConv conv)
        {
            textBoxSourceFilePath.Text = conv.SourceVideoFile;


            var vidList = conv.GetVideoTracks();
            if (vidList.Count > 0)
            {
                comboBoxVideoSelection.ItemsSource = vidList;
                comboBoxVideoSelection.DisplayMemberPath = "UIDescription";
                comboBoxVideoSelection.SelectedItem = conv.SelectedVideoTrack;
                tabItemVideo.Visibility = Visibility.Visible;
            }
            else
            {
                tabItemVideo.Visibility = Visibility.Collapsed;
                TabSourceDetails.SelectedIndex = 1;
            }

            // Update panel
            var vt = conv.SelectedVideoTrack;
            if (vt != null)
            {
                labelVBitRate.Content = String.Format("{0} Kbps", vt.BitRate > 0 ? vt.BitRate.ToString() : "N/A");
                labelRefFrames.Content = vt.ActualRefFrames.ToString();
                labelWidth.Content = vt.Width.ToString();
                labelHeight.Content = vt.Height.ToString();
                labelFPS.Content = vt.FPS.ToString();
                labelVideoRecode.Content = vt.RequiresRecode ? "Yes" : "No";
                checkBoxForceVideoRecode.IsChecked = conv.ForceVideoRecode;
            }

            var audList = conv.GetAudioTracks();
            if (audList.Count > 0)
            {
                comboBoxAudioSelection.ItemsSource = audList;
                comboBoxAudioSelection.DisplayMemberPath = "UIDescription";
                comboBoxAudioSelection.SelectedItem = conv.SelectedAudioTrack;
                tabItemAudio.Visibility = Visibility.Visible;
            }
            else
            {
                comboBoxAudioSelection.ItemsSource = null;
                tabItemAudio.Visibility = Visibility.Collapsed;
            }

            // Update panel
            var at = conv.SelectedAudioTrack;
            if (at != null)
            {
                labelABitRate.Content = String.Format("{0} Kbps", at.BitRate > 0 ? at.BitRate.ToString() : "N/A");
                labelAudioRecode.Content = at.RequiresRecode ? "Yes" : "No";
                labelNumChannels.Content = at.Channels;
            }

            var subList = conv.GetSubtitleTracks();
            if (subList.Count > 0)
            {
                comboBoxSubSelection.ItemsSource = subList;
                comboBoxSubSelection.DisplayMemberPath = "UIDescription";
                comboBoxSubSelection.SelectedItem = conv.SelectedSubTrack;
                SubtitlesTab.Visibility = Visibility.Visible;
            }
            else
            {
                comboBoxSubSelection.ItemsSource = null;
                SubtitlesTab.Visibility = Visibility.Hidden;
            }
        }

        public delegate void SetSelectedConvItemDelegate(PandaVideoConv conv);

        private void SetSelectedConvItem(PandaVideoConv conv)
        {
            listViewConversions.SelectedItem = conv;
            listViewConversions.ScrollIntoView(conv);
        }

        // This event handler is where the actual,
        // potentially time-consuming work is done.
        private void BackgroundWorker1DoWork(object sender, DoWorkEventArgs e)
        {
            // Get the BackgroundWorker that raised this event.

            // Assign the result of the computation
            // to the Result property of the DoWorkEventArgs
            // object. This is will be available to the 
            // RunWorkerCompleted eventhandler.
            _numFailures = 0;
            foreach (var conv in _convList)
            {
                listViewConversions.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                                                           new SetSelectedConvItemDelegate(SetSelectedConvItem), conv);

                bool result = conv.DiscMode ? conv.ConvertDisc() : conv.ConvertFile();

                e.Result = result;
                // for end of run status msg 
                if (!result)
                    _numFailures++;
            }
        }

        // This event handler deals with the results of the
        // background operation.
        private void BackgroundWorker1RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // First, handle the case where an exception was thrown.
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            buttonConvert.IsEnabled = true;
            tabControl1.IsEnabled = true;

            Mouse.OverrideCursor = Cursors.Arrow;

            logging.Add(new LogItem(DateTime.Now,
                                    String.Format("Converted {0} of {1} file(s) successfully, with {2} failures.",
                                                  _convList.Count - _numFailures, _convList.Count, _numFailures), null));
            // reset list
            _convList.Clear();
        }

        private void ButtonAddClick(object sender, RoutedEventArgs e)
        {
            string filter;
            var fDlg = new OpenFileDialog();

            if (comboBoxDevice.SelectedIndex == (int)OutputDevice.PS3)
                filter = "Video Files (*.mkv,*.mpg,*.m2ts,*.flv)|*.mkv;*.mpg;*.m2ts;*flv";
            else
            {
                filter =
                    "Video Files (*.ts,*.m2ts,*.mkv,*.mpg,*.mp4,*.flv,*.avi,*.vob,*.wmv) |*.ts;*.m2ts;*.mkv;*.mpg;*.mp4;*.flv;*.avi;*.vob;*.wmv";
                filter = filter + "|Audio Files (*.flac,*.mp3,*.ogg) | *.flac;*.mp3;*.ogg";
            }

            fDlg.Filter = filter + "|All Files|*.*";
            fDlg.CheckFileExists = true;
            fDlg.InitialDirectory = GetValidSourcePath(Settings.Default.SourceFolder);
            fDlg.Multiselect = true;
            if (fDlg.ShowDialog() == true)
            {
                AddFilesToList(fDlg.FileNames);
            }
        }

        private void ButtonClearClick(object sender, RoutedEventArgs e)
        {
            _convList.Clear();
            _convSelection = null;
        }

        private void ButtonDelClick(object sender, RoutedEventArgs e)
        {
            if (listViewConversions.SelectedIndex != -1)
            {
                _convList.RemoveAt(listViewConversions.SelectedIndex);
            }
        }

        private void ButtonAddDiscClick(object sender, RoutedEventArgs e)
        {
            var fileDlg = new FolderBrowserDialog
                {
                    SelectedPath = Path.GetDirectoryName(Settings.Default.SourceFolder),
                    Tag = "Select a DVD Folder location"
                };

            if (fileDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var convertFile = new PandaVideoConv();
                convertFile.StatusChanged += new ChangedEventHandler(ConvStatusChanged);
                // set the output prop
                convertFile.OutputFolder = textBoxOutputFilePath.Text;

                convertFile.SelectedDevice = (IOutputDevice)comboBoxDevice.SelectedValue;
                convertFile.PrefferedLanguage = (comboBoxPrefAudioLang.SelectedItem as AudioLanguage).Lang;

                AnalyseFolder(convertFile, fileDlg.SelectedPath);


                _convList.Add(convertFile);

                listViewConversions.SelectedIndex = listViewConversions.Items.Count - 1;
                Settings.Default.SourceFolder = fileDlg.SelectedPath;
            }
        }

        private void ButtonWorkFolderBrowseClick(object sender, RoutedEventArgs e)
        {
            var fileDlg = new FolderBrowserDialog
                {
                    SelectedPath = Path.GetDirectoryName(Settings.Default.SourceFolder),
                    Tag = "Select a Working Folder location"
                };

            if (fileDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxWorkingFolder.Text = fileDlg.SelectedPath;
            }
        }

        private void ButtonOutputBrowseClick(object sender, RoutedEventArgs e)
        {
            var fileDlg = new FolderBrowserDialog
                {
                    SelectedPath = textBoxOutputFilePath.Text,
                    Tag = "Select a folder location"
                };

            if (fileDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxOutputFilePath.Text = fileDlg.SelectedPath;
            }
        }

        private void ButtonConvertClick(object sender, RoutedEventArgs e)
        {
            // set the output prop
            _convSelection.OutputFolder = textBoxOutputFilePath.Text;
            _convSelection.WorkingFolder = textBoxWorkingFolder.Text;

            Mouse.OverrideCursor = Cursors.Wait;
            buttonConvert.IsEnabled = false;
            tabControl1.IsEnabled = false;
            _backgroundWorker1.RunWorkerAsync();
        }


        private void ComboBoxAudioSelectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxAudioSelection.SelectedIndex != -1)
            {
                _convSelection.SelectedAudioTrack = (AudioTrack)comboBoxAudioSelection.SelectedItem;
                UpdatePanel(_convSelection);
            }
        }

        private void ComboBoxVideoSelectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxVideoSelection.SelectedIndex != -1)
            {
                _convSelection.SelectedVideoTrack = (VideoTrack)comboBoxVideoSelection.SelectedItem;
                UpdatePanel(_convSelection);
            }
        }

        private void ComboBoxSubSelectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxSubSelection.SelectedIndex != -1)
            {
                _convSelection.SelectedSubTrack = (SubTrack)comboBoxSubSelection.SelectedItem;
                UpdatePanel(_convSelection);
            }
        }

        private void ComboBoxDeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxDevice.SelectedIndex == -1) return;
            var device = (comboBoxDevice.SelectedItem as IOutputDevice);
            if (device == null) return;
            image1.Source = device.GetImage;
            if (device.Ringtone)
            {
                checkBoxRingtone.Visibility = Visibility.Visible;
                checkBoxRingtone.IsEnabled = true;
            }
            else
            {
                checkBoxRingtone.Visibility = Visibility.Hidden;
                checkBoxRingtone.IsEnabled = false;
                checkBoxRingtone.IsChecked = false;
            }

            // HEVC
            if (device.HEVC)
            {
                checkBoxUseHEVC.Visibility = Visibility.Visible;
                checkBoxUseHEVC.IsEnabled = true;
            }
            else
            {
                checkBoxUseHEVC.Visibility = Visibility.Hidden;
                checkBoxUseHEVC.IsEnabled = false;
                checkBoxUseHEVC.IsChecked = false;
            }


        }


        private void ListViewLogMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listViewLog.SelectedIndex != -1)
            {
                var item = (LogItem)listViewLog.SelectedItem;

                if (item.Data != null)
                {
                    var dlg = new ShowOutput { Output = item.Data.outputReport };
                    dlg.ShowDialog();
                }
            }
        }

        private void ListViewConversionsPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;

            // Mark the event as handled, so listview's native DragOver handler is not called.
            e.Handled = true;
        }

        private void ListViewConversionsPreviewDrop(object sender, DragEventArgs e)
        {
            var droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

            AddFilesToList(droppedFilePaths);
        }

        private void ListViewConversionsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listViewConversions.SelectedIndex != -1)
            {
                tabItem1.IsEnabled = true;
                _convSelection = _convList[listViewConversions.SelectedIndex];
                UpdatePanel(_convSelection);
            }
            else
                tabItem1.IsEnabled = false;
        }

        // Log Context menu
        private void ListViewLogContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (listViewLog.SelectedIndex != -1)
            {
                var item = (LogItem)listViewLog.SelectedItem;

                if (item.Data != null)
                {
                    MenuShowReport.IsEnabled = true;
                    if (!String.IsNullOrEmpty(item.Data.outputFileName))
                    {
                        MenuPreviewFile.IsEnabled = true;
                        MenuShowExplorer.IsEnabled = true;
                    }
                    else
                    {
                        MenuPreviewFile.IsEnabled = false;
                        MenuShowExplorer.IsEnabled = false;
                    }
                }
                else
                {
                    MenuShowReport.IsEnabled = false;
                }
            }
        }

        // Actions
        private void AnalyseFile(PandaVideoConv conv, string sourceFilePath)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // make sure its still there
                if (File.Exists(sourceFilePath))
                {
                    conv.DiscMode = false;

                    conv.AnalyseFile(sourceFilePath);


                    // Allow convert
                    buttonConvert.IsEnabled = true;
                }
                else
                    logging.Add(new LogItem("Failed to find file: " + textBoxSourceFilePath.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PVC failure");
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }

        private void AnalyseFolder(PandaVideoConv conv, string sourceFilePath)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // make sure its still there

                if (Directory.Exists(sourceFilePath))
                {
                    conv.DiscMode = true;
                    conv.AnalyseFile(sourceFilePath);


                    // Allow convert
                    buttonConvert.IsEnabled = true;
                }
                else
                    logging.Add(new LogItem("Failed to find folder: " + textBoxSourceFilePath.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PVC failure");
            }
            finally
            {
                Mouse.OverrideCursor = Cursors.Arrow;
            }
        }

        private bool AddFilesToList(IEnumerable<string> filelist)
        {
            foreach (var filePath in filelist)
            {
                var convertFile = new PandaVideoConv();
                convertFile.StatusChanged += ConvStatusChanged;
                // set the output prop
                convertFile.OutputFolder = textBoxOutputFilePath.Text;
                convertFile.SelectedDevice = (IOutputDevice)comboBoxDevice.SelectedValue;

                // Apple ringtone support
                convertFile.RingTone = (bool)checkBoxRingtone.IsChecked;

                // Encode subtitles 
                convertFile.EncodeSubtitles = (bool)checkBoxEncodeSubs.IsChecked;
                convertFile.HEVCRecode = (bool)checkBoxUseHEVC.IsChecked;

                convertFile.PrefferedLanguage = (comboBoxPrefAudioLang.SelectedItem as AudioLanguage).Lang;

                // full analysis
                AnalyseFile(convertFile, filePath);

                // add
                _convList.Add(convertFile);

                listViewConversions.SelectedIndex = listViewConversions.Items.Count - 1;
                Settings.Default.SourceFolder = filePath;
            }

            Settings.Default.Save();
            return true;
        }


        /// <summary>
        ///   Update call to see if newer versions are available
        /// </summary>
        /// <param name="sender"> </param>
        /// <param name="e"> </param>
        private void CheckUpdatesClick(object sender, RoutedEventArgs e)
        {
            checkBoxAutoCheckUpdate.IsEnabled = false;
            if (!UpdaterManualCheck())
                MessageBox.Show("No updates available");
            checkBoxAutoCheckUpdate.IsEnabled = true;
        }

        private bool UpdaterCheck()
        {
            _updater = new Updater("Pandasoft Video Converter.",
                                   InfoUrl,
                                   Description,
                                   ManifestUrl,
                                   PublicKeyXml);
            return _updater.Status.IsUpdating;
        }

        private bool UpdaterManualCheck()
        {
            _updater = new Updater("Pandasoft Video Converter.",
                                   InfoUrl,
                                   Description,
                                   ManifestUrl,
                                   PublicKeyXml);
            return _updater.IsUpdateAvailable();
        }

        private void CheckBoxForceVideoRecodeClick(object sender, RoutedEventArgs e)
        {
            _convSelection.ForceVideoRecode = (bool)checkBoxForceVideoRecode.IsChecked;
        }

        private String GetValidSourcePath(String path)
        {
            String directoryName = Path.GetDirectoryName(path);
            if (Directory.Exists(directoryName))
                return directoryName;
            return String.Empty;
        }


        private void MenuItemClick(object sender, RoutedEventArgs e)
        {
            if (listViewLog.SelectedIndex == -1) return;
            var item = (LogItem)listViewLog.SelectedItem;

            if (item.Data == null) return;
            var dlg = new ShowOutput { Output = item.Data.outputReport };
            dlg.ShowDialog();
        }

        private void MenuItemClickPreview(object sender, RoutedEventArgs e)
        {
            if (listViewLog.SelectedIndex == -1) return;
            var item = (LogItem)listViewLog.SelectedItem;

            if (item.Data != null &&
                !String.IsNullOrEmpty(item.Data.outputFileName))
            {
                var process = new Process
                    {
                        StartInfo =
                            {
                                FileName = "explorer.exe",
                                Arguments = item.Data.outputFileName,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true
                            }
                    };

                Debug.WriteLine(process.StartInfo.Arguments);
                process.Start();
            }
        }

        private void MenuItemClickShowExplorer(object sender, RoutedEventArgs e)
        {
            if (listViewLog.SelectedIndex != -1)
            {
                var item = (LogItem)listViewLog.SelectedItem;

                if (item.Data != null &&
                    !String.IsNullOrEmpty(item.Data.outputFileName))
                {
                    var process = new Process
                        {
                            StartInfo =
                                {
                                    FileName = "explorer.exe",
                                    Arguments = Path.GetDirectoryName(item.Data.outputFileName),
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true
                                }
                        };

                    Debug.WriteLine(process.StartInfo.Arguments);
                    process.Start();
                }
            }
        }

        // Conversion Context menu
        private void BatchMenuItemClickPreview(object sender, RoutedEventArgs e)
        {
            if (listViewConversions.SelectedIndex != -1)
            {
                var item = (PandaVideoConv)listViewConversions.SelectedItem;

                if (item != null &&
                    !String.IsNullOrEmpty(item.SourceVideoFile))
                {
                    var process = new Process
                        {
                            StartInfo =
                                {
                                    FileName = "explorer.exe",
                                    Arguments = item.SourceVideoFile,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true
                                }
                        };

                    Debug.WriteLine(process.StartInfo.Arguments);
                    process.Start();
                }
            }
        }

        private void BatchMenuItemClickShowExplorer(object sender, RoutedEventArgs e)
        {
            if (listViewConversions.SelectedIndex != -1)
            {
                var item = (PandaVideoConv)listViewConversions.SelectedItem;

                if (item != null &&
                    !String.IsNullOrEmpty(item.SourceVideoFile))
                {
                    var process = new Process
                        {
                            StartInfo =
                                {
                                    FileName = "explorer.exe",
                                    Arguments = Path.GetDirectoryName(item.SourceVideoFile),
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true
                                }
                        };

                    Debug.WriteLine(process.StartInfo.Arguments);
                    process.Start();
                }
            }
        }

        private void checkBoxUseHEVC_Click(object sender, RoutedEventArgs e)
        {
            _convSelection.HEVCRecode = checkBoxUseHEVC.IsChecked.GetValueOrDefault(false);
        }
    }
}