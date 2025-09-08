using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Linq;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO;
using S16.Commands;
using SubtitleTools.UI.ViewModels;
using System.ComponentModel;

namespace SubtitleTools.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        private readonly MainViewModel model;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            model = new MainViewModel(this);
            DataContext = model;
            
            model.RecentFilesMenu.Initialize(menuRecentFiles);

            model.FileLoaded += Model_FileLoaded;
            model.FileClosed += Model_FileClosed;

            CommandBindings.Add(new CommandBindingLink(ApplicationCommands.New, model.NewCommand));
            CommandBindings.Add(new CommandBindingLink(ApplicationCommands.Open, model.OpenCommand));
            CommandBindings.Add(new CommandBindingLink(ApplicationCommands.Save, model.SaveCommand));
            CommandBindings.Add(new CommandBindingLink(ApplicationCommands.SaveAs, model.SaveAsCommand));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, ExitCommand_Executed));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Help, AboutCommand_Executed));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, PrintCommand_Executed, Print_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.PrintPreview, PrintPreview_Executed, Print_CanExecute));

            Loaded += MainWindow_Loaded;
        }
        #endregion

        #region Methods
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            testText.Text = @"The <b>quick <i>brown</i> fox jump <font color=""#ff0000"">over</font></b> <u>lazy</u> dog.";
            model.Initialize();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = model.SaveConfirm();
            if (!e.Cancel)
            {
                model.Dispose();
            }
            base.OnClosing(e);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data != null)
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data != null)
            {
                string[] files = (string[])e.Data.GetData("FileDrop");
                if ((files != null) && (files.Length > 0))
                {
                    //
                }
            }
        }

        private void Model_FileLoaded(object sender, EventArgs e)
        {
            Title = $"Subtitle Tools - {model.CurrentFileName}";
            CommandManager.InvalidateRequerySuggested();
        }

        private void Model_FileClosed(object sender, EventArgs e)
        {
            Title = "Subtitle Tools";
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void AboutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new AboutDialog()
            {
                Owner = this
            };
            dialog.ShowDialog();
        }

        private void Print_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = model.HasData;
        }

        private void PrintCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PrintDialog dialog = new PrintDialog();

            double totalWidth = model.GetDocumentWidth();
            FlowDocument document = model.BuildDocument();
            
            if (dialog.ShowDialog() == true)
            {
                double areaWidth = Math.Min(dialog.PrintableAreaWidth, totalWidth);
                document.ColumnWidth = areaWidth;

                IDocumentPaginatorSource documentSource = document as IDocumentPaginatorSource;
                var paginator = documentSource.DocumentPaginator;

                dialog.PrintDocument(paginator, model.CurrentFileName);
            }
        }

        private void PrintPreview_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Controls.PrintPreviewDialog dialog = new Controls.PrintPreviewDialog();

            FlowDocument document = model.BuildDocument();
            dialog.Document = document;
            if (dialog.ShowDialog(this, "Print preview") == true)
            {
                //
            }
        }
        #endregion
    }
}
