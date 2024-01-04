using Microsoft.Win32;
using S16.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SubtitleTools.UI.ViewModels
{
    public class ReplaceTextViewModel : S16.Observable.ObservableObject
    {
        #region Variables
        private OpenFileDialog openFileDialog = new()
        {
            Title = "Open file",
            Filter = "Subrip File|*.srt|All File|*.*",
            CheckFileExists = true
        };

        private SaveFileDialog saveFileDialog = new SaveFileDialog()
        {
            OverwritePrompt = true,
            AddExtension = true,
            Filter = "Subrip File|*.srt|All File|*.*",
        };

        private readonly Command openTimeCommand;
        private readonly Command openTextCommand;
        private readonly Command saveCommand;

        private ObservableSubtitle timeSubtitle = new ObservableSubtitle();
        private int timeEditIndex = -1;
        private ObservableSubtitle textSubtitle = new ObservableSubtitle();
        private int textEditIndex = -1;

        private string currentTimeFile = string.Empty;
        private string currentTextFile = string.Empty;

        private bool isEdited = false;
        private bool isProcessing = false;

        private bool syncScroll = false;
        private Window window = null;
        private int timeSelectionCount = 0;
        private int textSelectionCount = 0;
        #endregion

        #region Events
        public event EventHandler IsEditChanged = null;
        #endregion

        #region Constructors
        public ReplaceTextViewModel()
        {
            openTimeCommand = new Command(OnOpenTimeFileExecuted);
            openTextCommand = new Command(OnOpenTtextFileExecuted);
            saveCommand = new Command(OnSaveExecuted, () => IsEdited);
        }
        #endregion

        #region Properties
        public Window Window
        {
            set { window = value; }
        }

        public ICommand OpenTimeCommand
        {
            get => openTimeCommand;
        }

        public ICommand OpenTextCommand
        {
            get => openTextCommand;
        }

        public ICommand SaveCommand
        {
            get => saveCommand;
        }

        public bool IsProcessing
        {
            get => isProcessing;
            set { SetProperty(ref isProcessing, value); }
        }

        public string CurrentTimeFile
        {
            get => currentTimeFile;
            protected set
            {
                SetProperty(ref currentTimeFile, value);
                RaisePropertyChanged("CurrentTimeFileName");
            }
        }

        public string CurrentTimeFileName
        {
            get
            {
                if (string.IsNullOrEmpty(currentTimeFile)) return string.Empty;
                return Path.GetFileName(currentTimeFile);
            }
        }

        public string CurrentTextFile
        {
            get => currentTextFile;
            protected set
            {
                SetProperty(ref currentTextFile, value);
                RaisePropertyChanged("CurrentTextFileName");
            }
        }

        public string CurrentTextFileName
        {
            get
            {
                if (string.IsNullOrEmpty(currentTextFile)) return string.Empty;
                return Path.GetFileName(currentTextFile);
            }
        }

        public bool IsEdited
        {
            get => isEdited;
            set 
            {
                SetProperty(ref isEdited, value, nameof(IsEdited), () => 
                {
                    IsEditChanged?.Invoke(this, EventArgs.Empty);
                });
            }
        }

        public bool IsLoaded
        {
            get => !string.IsNullOrEmpty(currentTimeFile) && !string.IsNullOrEmpty(currentTextFile);
        }

        public ObservableSubtitle TimeSource
        {
            get => timeSubtitle;
            set { SetProperty(ref timeSubtitle, value); }
        }

        public int TimeEditIndex
        {
            get => timeEditIndex;
            set 
            { 
                SetProperty(ref timeEditIndex, value, nameof(TimeEditIndex), () => 
                {
                    RaisePropertyChanged("TimeEditDialogue");
                }); 
            }
        }

        public Dialogue TimeEditDialogue
        {
            get
            {
                if (timeEditIndex > -1 && timeSubtitle.Count > timeEditIndex)
                {
                    return timeSubtitle[timeEditIndex];
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    timeSubtitle[timeEditIndex].Id = value.Id;
                    timeSubtitle[timeEditIndex].StartTime = value.StartTime;
                    timeSubtitle[timeEditIndex].EndTime = value.EndTime;
                    timeSubtitle[timeEditIndex].Text = value.Text;
                }
            }
        }

        public ObservableSubtitle TextSource
        {
            get => textSubtitle;
            set { SetProperty(ref textSubtitle, value); }
        }

        public int TextEditIndex
        {
            get => textEditIndex;
            set
            {
                SetProperty(ref textEditIndex, value, nameof(TextEditIndex), () =>
                {
                    RaisePropertyChanged("TextEditDialogue");
                });
            }
        }

        public Dialogue TextEditDialogue
        {
            get
            {
                if (textEditIndex > -1 && textSubtitle.Count > textEditIndex)
                {
                    return textSubtitle[textEditIndex];
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    textSubtitle[textEditIndex].Id = value.Id;
                    textSubtitle[textEditIndex].StartTime = value.StartTime;
                    textSubtitle[textEditIndex].EndTime = value.EndTime;
                    textSubtitle[textEditIndex].Text = value.Text;
                }
            }
        }

        public bool SyncScroll
        {
            get => syncScroll;
            set { SetProperty(ref syncScroll, value); }
        }

        public int TimeSelectionCount
        {
            get => timeSelectionCount;
            set { SetProperty(ref timeSelectionCount, value); }
        }

        public int TextSelectionCount
        {
            get => textSelectionCount;
            set { SetProperty(ref textSelectionCount, value); }
        }
        #endregion

        #region Methods
        public void OnOpenTimeFileExecuted()
        {
            if (openFileDialog.ShowDialog(window) == true)
            {
                LoadTimeFile(openFileDialog.FileName);
            }
        }

        public void OnOpenTtextFileExecuted()
        {
            if (openFileDialog.ShowDialog(window) == true)
            {
                LoadTextFile(openFileDialog.FileName);
            }
        }

        public async void LoadTimeFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            IsProcessing = true;
            timeSubtitle.Clear();

            var result = await Task.Run(() => {

                ObservableSubtitle subtitle = null;
                try
                {
                    subtitle = ObservableSubtitle.FromFile(fileName);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }

                return subtitle;
            });

            if (result != null)
            {
                CurrentTimeFile = fileName;
                TimeSource = result;
            }

            IsProcessing = false;
            IsEdited = false;
        }

        public async void LoadTextFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            IsProcessing = true;
            textSubtitle.Clear();

            var result = await Task.Run(() => {

                ObservableSubtitle subtitle = null;
                try
                {
                    subtitle = ObservableSubtitle.FromFile(fileName);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }

                return subtitle;
            });

            if (result != null)
            {
                CurrentTextFile = fileName;
                TextSource = result;
            }

            IsProcessing = false;
        }

        public void OnSaveExecuted()
        {
            if (string.IsNullOrEmpty(CurrentTimeFile))
            {
                OnSaveAsExecuted();
                return;
            }
            SaveFile();
        }

        public void OnSaveAsExecuted()
        {
            if (saveFileDialog.ShowDialog(window) == true)
            {
                SaveFile(saveFileDialog.FileName);
            }
        }

        public void SaveFile(string fileName = null)
        {
            if (!IsLoaded) return;

            string filePath = string.IsNullOrEmpty(fileName) ? currentTimeFile : fileName;
            var encoding = Encoding.UTF8;
            var dispatcher = window.Dispatcher;

            IsProcessing = true;
            var task = Task.Run(async () =>
            {
                try
                {
                    using (var sw = new StreamWriter(filePath, false, encoding, 1024))
                    {
                        var text = timeSubtitle.ToString();
                        await sw.WriteAsync(text);
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            });

            task.GetAwaiter().OnCompleted(() =>
            {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    IsProcessing = false;
                    IsEdited = false;
                });
            });
        }

        public bool SaveConfirm()
        {
            if (IsEdited)
            {
                var result = MessageBox.Show("Do you want to save changes?", "Subtitle Tools", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        {
                            string fileName = string.Empty;
                            if (!IsLoaded && saveFileDialog.ShowDialog(window) == true)
                            {
                                fileName = saveFileDialog.FileName;
                            }
                            SaveFile(fileName);
                        }
                        break;
                    case MessageBoxResult.Cancel:
                        return true;
                    default:
                        break;
                }
            }
            return false;
        }

        public void ReplaceText(IEnumerable<Dialogue> times, IEnumerable<Dialogue> texts)
        {
            if (times == null || texts == null) return;

            List<int> timeIndexes = new List<int>();
            foreach(var t in times)
            {
                var idx = timeSubtitle.IndexOf(t);
                if (idx > -1) timeIndexes.Add(idx);
            }
            timeIndexes.Sort();

            List<int> textIndexes = new List<int>();
            foreach (var t in texts)
            {
                var idx = textSubtitle.IndexOf(t);
                if (idx > -1) textIndexes.Add(idx);
            }
            textIndexes.Sort();

            if (timeIndexes.Count != textIndexes.Count)
            {
                var result = MessageBox.Show("Selection are not equal.\nAre you sure to replace?", "Subtitle Tools", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.Cancel) return;
            }

            int i = 0;
            foreach (var ti in timeIndexes)
            {
                timeSubtitle[ti].Text = textSubtitle[textIndexes[i]].Text;
                i++;
                if (i == textIndexes.Count) break;
            }

            RaisePropertyChanged("TimeEditText");
            IsEdited = true;
        }
        #endregion
    }
}
