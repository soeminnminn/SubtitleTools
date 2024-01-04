using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;

namespace SubtitleTools.UI.Controls
{
    public class PrintPreviewDialog : DependencyObject
    {
        #region Variables
        private readonly Window _windowProvider;
        private readonly DocumentViewer _documentViewer;

        private string _tempFileName = null;
        #endregion

        #region Constructor
        public PrintPreviewDialog()
        {
            _windowProvider = new Window();
            _documentViewer = new DocumentViewer();

            _windowProvider.ShowInTaskbar = false;
            _windowProvider.SetValue(Window.ContentProperty, _documentViewer);

            _windowProvider.Closing += Window_Closing;
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
            nameof(Document), typeof(FlowDocument), typeof(PrintPreviewDialog),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnDocumentChanged)));

        public FlowDocument Document
        {
            get => (FlowDocument)GetValue(DocumentProperty);
            set { SetValue(DocumentProperty, value); }
        }

        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PrintPreviewDialog c)
            {
                c.OnDocumentChanged(e.NewValue as FlowDocument);
            }
        }
        #endregion

        #region Methods
        private void OnDocumentChanged(FlowDocument document)
        {
            if (document != null)
            {
                LoadFlowDocument(document);
            }
        }

        private void LoadFlowDocument(FlowDocument flowDocument)
        {
            DocumentPaginator paginator = ((IDocumentPaginatorSource)flowDocument).DocumentPaginator;

            string tempFileName = Path.GetTempFileName();
            if (File.Exists(tempFileName))
                File.Delete(tempFileName);

            using (XpsDocument xpsDoc = new XpsDocument(tempFileName, FileAccess.ReadWrite))
            {
                XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                
                xpsWriter.Write(paginator);
                _documentViewer.Document = xpsDoc.GetFixedDocumentSequence();

                xpsDoc.Close();
            }
            _tempFileName = tempFileName;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(_tempFileName) && File.Exists(_tempFileName))
                File.Delete(_tempFileName);
        }

        public bool? ShowDialog(Window owner, string title)
        {
            _windowProvider.SetValue(Window.TitleProperty, title);
            _windowProvider.Owner = owner;
            return _windowProvider.ShowDialog();
        }
        #endregion
    }
}
