using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using S16.Commands;
using SubtitleTools.UI.Models;
using SubtitleTools.UI.RecentFiles;

namespace SubtitleTools.UI.ViewModels
{
    public class MainViewModel : S16.Observable.ObservableObject
    {
        // https://github.com/videolan/libvlcsharp/blob/3.x/samples/LibVLCSharp.WinForms.Sample/Form1.cs

        #region Variables
        public static readonly string[] EXTENSIONS = new string[] { ".srt" };

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

        private readonly Window mainWindow;
        private readonly IRecentFilesMenu recentFilesMenu;

        private string currentFile = string.Empty;
        private Encoding encoding = Encoding.UTF8;
        private ObservableSubtitle items = new ObservableSubtitle();

        private readonly Command newCommand;
        private readonly Command openCommand;
        private readonly Command saveCommand;
        private readonly Command saveAsCommand;

        private readonly Command cleanCommand;
        private readonly Command replaceTextCommand;

        private readonly ConfigModel config;
        private PlayerModel player = null;

        private bool showToolbar = true;
        private bool showStatusbar = true;

        private bool isEdited = false;
        private bool _isProcessing = false;
        private int editDialogIndex = -1;

        private static readonly TableDocumentWriter.Column[] columns = new TableDocumentWriter.Column[]
        {
            new TableDocumentWriter.Column() { Width = 50, Header = "Id", HeaderAlignment = TextAlignment.Center, CellAlignment = TextAlignment.Right },
            new TableDocumentWriter.Column() { Width = 100, Header = "Start", HeaderAlignment = TextAlignment.Center, CellAlignment = TextAlignment.Center },
            new TableDocumentWriter.Column() { Width = 100, Header = "End", HeaderAlignment = TextAlignment.Center, CellAlignment = TextAlignment.Center },
            new TableDocumentWriter.Column() { Width = 500, Header = "Dialouge", HeaderAlignment = TextAlignment.Center, CellAlignment = TextAlignment.Left },
        };
        #endregion

        #region Events
        public event EventHandler FileLoaded = null;
        public event EventHandler FileClosed = null;
        #endregion

        #region Constructors
        public MainViewModel()
            : this(Application.Current.MainWindow)
        { }

        public MainViewModel(Window window)
        {
            mainWindow = window;

            config = new ConfigModel();

            recentFilesMenu = new RecentFilesMenu(config);
            recentFilesMenu.RecentFileSelected += RecentFileSelected;

            newCommand = new Command(OnNewFileExecuted);
            openCommand = new Command(OnOpenFileExecuted);
            saveCommand = new Command(OnSaveExecuted, () => IsEdited);
            saveAsCommand = new Command(OnSaveAsExecuted, () => IsLoaded);

            cleanCommand = new Command(OnCleanExecuted, () => HasData);
            replaceTextCommand = new Command(OnReplaceTextExecuted);
        }
        #endregion

        #region Properties
        public IRecentFilesMenu RecentFilesMenu
        {
            get => recentFilesMenu;
        }

        public ICommand NewCommand 
        { 
            get => newCommand; 
        }

        public ICommand OpenCommand
        {
            get => openCommand;
        }

        public ICommand SaveCommand 
        { 
            get => saveCommand; 
        }

        public ICommand SaveAsCommand 
        { 
            get => saveAsCommand; 
        }

        public ICommand CleanCommand
        {
            get => cleanCommand;
        }

        public ICommand ReplaceTextCommand
        {
            get => replaceTextCommand;
        }

        public string CurrentFile
        {
            get => currentFile;
            protected set 
            { 
                SetProperty(ref currentFile, value);
                RaisePropertyChanged(nameof(CurrentFileName), nameof(IsLoaded));
            }
        }

        public string CurrentFileName
        {
            get
            {
                if (string.IsNullOrEmpty(currentFile)) return string.Empty;
                return Path.GetFileName(currentFile);
            }
        }

        public Encoding Encoding 
        { 
            get => encoding; 
        }

        public bool IsLoaded 
        { 
            get => !string.IsNullOrEmpty(currentFile); 
        }

        public bool IsEdited
        {
            get => isEdited;
            private set { SetProperty(ref isEdited, value); }
        }

        public bool HasData 
        { 
            get => items.Count > 0; 
        }

        public int Count => items.Count;

        public ObservableSubtitle ItemsSource
        {
            get => items;
            set 
            { 
                SetProperty(ref items, value);
                RaisePropertyChanged(nameof(HasData));
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set { SetProperty(ref _isProcessing, value); }
        }

        public bool ShowToolbar
        {
            get => showToolbar;
            set { SetProperty(ref showToolbar, value); }
        }

        public bool ShowStatusbar
        {
            get => showStatusbar;
            set { SetProperty(ref showStatusbar, value); }
        }

        public int EditDialogIndex
        {
            get => editDialogIndex;
            set 
            {
                SetProperty(ref editDialogIndex, value);
                RaisePropertyChanged("EditingDialog");
            }
        }

        public Dialogue EditingDialog
        {
            get
            {
                if (editDialogIndex > -1 && items.Count > editDialogIndex)
                {
                    return items[editDialogIndex];
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    items[editDialogIndex].Id = value.Id;
                    items[editDialogIndex].StartTime = value.StartTime;
                    items[editDialogIndex].EndTime = value.EndTime;
                    items[editDialogIndex].Text = value.Text;
                    IsEdited = true;
                }
            }
        }
        #endregion

        #region Methods
        public static bool IsSupportedFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            try
            {
                var ext = Path.GetExtension(fileName);
                if (ext != null) return Array.Exists(EXTENSIONS, x => x == ext.ToLowerInvariant());
            }
            catch { }

            return false;
        }

        public async void Initialize()
        {
            await config.LoadAsync();

            player = new PlayerModel(config);

            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1)
            {
                var filePath = args.Where(f => IsSupportedFile(f)).ToArray().FirstOrDefault();
                if (!string.IsNullOrEmpty(filePath))
                {
                    LoadFile(filePath);
                }
            }
        }

        public void OnNewFileExecuted()
        {
            CloseFile();
        }

        public void OnOpenFileExecuted()
        {
            if (openFileDialog.ShowDialog(mainWindow) == true)
            {
                LoadFile(openFileDialog.FileName);
            }
        }

        public void OnSaveExecuted()
        {
            if (string.IsNullOrEmpty(CurrentFile))
            {
                OnSaveAsExecuted();
                return;
            }
            SaveFile();
        }

        public void OnSaveAsExecuted()
        {
            if (saveFileDialog.ShowDialog(mainWindow) == true)
            {
                SaveFile(saveFileDialog.FileName);
            }
        }

        public void OnFileDrop(string[] files)
        {
            if (files != null && files.Length > 0)
            {
                var filePath = files.FirstOrDefault();
                if (!string.IsNullOrEmpty(filePath))
                {
                    LoadFile(filePath);
                }
            }
        }

        public async void LoadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            IsProcessing = true;
            items.Clear();

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
                CurrentFile = fileName;
                ItemsSource = result;

                recentFilesMenu.AddRecentFile(fileName);
            }

            IsProcessing = false;
            FileLoaded?.Invoke(this, EventArgs.Empty);
        }

        public void CloseFile()
        {
            if (string.IsNullOrEmpty(currentFile)) return;

            items.Clear();
            CurrentFile = string.Empty;
            IsEdited = false;

            FileClosed?.Invoke(this, EventArgs.Empty);
        }

        public void SaveFile(string fileName = null)
        {
            if (!IsLoaded) return;

            string filePath = string.IsNullOrEmpty(fileName) ? currentFile : fileName;
            var encoding = Encoding;
            var dispatcher = mainWindow.Dispatcher;

            IsProcessing = true;
            var task = Task.Run(async () =>
            {
                try
                {
                    using (var sw = new StreamWriter(filePath, false, encoding, 1024))
                    {
                        var text = items.ToString();
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
                            if (!IsLoaded && saveFileDialog.ShowDialog(mainWindow) == true)
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

        private void RecentFileSelected(object sender, RecentFileSelectedEventArgs e)
        {
            LoadFile(e.FileName);
        }

        private void OnCleanExecuted()
        {
            var dispatcher = mainWindow.Dispatcher;

            IsProcessing = true;
            ISubtitle subtitle = items;
            Cleaner cleaner = new Cleaner();
            cleaner.Clean(ref subtitle);

            Task.Factory.StartNew(() => 
            {
                Thread.Sleep(100);

                dispatcher.BeginInvoke(DispatcherPriority.Normal, () => 
                {
                    IsProcessing = false;
                    FileLoaded?.Invoke(this, EventArgs.Empty);
                    IsEdited = true;
                });
            });
        }

        private void OnReplaceTextExecuted()
        {
            var model = new ReplaceTextViewModel();
            var dialog = new Views.ReplaceTextWindow();
            model.Window = dialog;
            dialog.DataContext = model;
            dialog.Show();
        }

        public async void Dispose()
        {
            recentFilesMenu.Dispose();
            if (player != null)
            {
                player.Dispose();
            }

            await config.SaveAsync();
        }

        public FlowDocument BuildDocument()
        {
            var list = ItemsSource.Select(d => new string[] { d.Id, d.Start, d.End, d.Text });
            FlowDocument document = null;
            using (var docWriter = new TableDocumentWriter())
            {
                docWriter.Render(columns, list);
                document = docWriter.ToFlowDocument();
                document.ColumnWidth = GetDocumentWidth();
            }

            return document;
        }

        public double GetDocumentWidth()
        {
            double totalWidth = 0.0;
            foreach (var col in columns)
            {
                totalWidth += col.Width;
            }
            return totalWidth;
        }
        #endregion
    }
}
