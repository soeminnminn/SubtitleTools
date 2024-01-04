using SubtitleTools.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace SubtitleTools.UI.Views
{
    /// <summary>
    /// Interaction logic for ReplaceTextWindow.xaml
    /// </summary>
    public partial class ReplaceTextWindow : Window
    {
        #region Variables
        private bool isScrolling = false;
        private Controls.ListBoxEx selectedList = null;
        #endregion

        #region Constructors
        public ReplaceTextWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region Properties
        private ViewModels.ReplaceTextViewModel Model
        {
            get
            {
                if (DataContext is ViewModels.ReplaceTextViewModel model)
                    return model;

                return null;
            }
        }
        #endregion

        #region Methods
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (Model != null)
            {
                Model.IsEditChanged += Model_IsEditChanged;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Model != null)
            {
                e.Cancel = Model.SaveConfirm();
            }            
            base.OnClosing(e);
        }

        private void Model_IsEditChanged(object sender, EventArgs e)
        {
            if (sender is ReplaceTextViewModel model)
            {
                Title = model.IsEdited ? "Replace Text - [Edited]" : "Replace Text";
            }
        }

        private void listTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listTime.SelectedItem is Dialogue && Model != null)
            {
                Model.TimeSelectionCount = listTime.SelectedItems.Count;
                Model.TimeEditIndex = listTime.SelectedIndex;
            }
            // SystemColors.InactiveSelectionHighlightBrush
            CommandManager.InvalidateRequerySuggested();
        }

        private void listTime_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (isScrolling) return;

            if (Model.SyncScroll)
            {
                isScrolling = true;

                var scrollViewer = listText.GetScrollHost();
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
                }

                isScrolling = false;
            }
        }

        private void listTime_GotFocus(object sender, RoutedEventArgs e)
        {
            selectedList = listTime;
            CommandManager.InvalidateRequerySuggested();
        }

        private void listTime_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            selectedList = listTime;
            CommandManager.InvalidateRequerySuggested();
        }

        private void listText_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listText.SelectedItem is Dialogue && Model != null)
            {
                Model.TextSelectionCount = listText.SelectedItems.Count;
                Model.TextEditIndex = listText.SelectedIndex;
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void listText_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (isScrolling) return;

            if (Model.SyncScroll)
            {
                isScrolling = true;

                var scrollViewer = listTime.GetScrollHost();
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
                }

                isScrolling = false;
            }
        }

        private void listText_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            selectedList = listText;
            CommandManager.InvalidateRequerySuggested();
        }

        private void listText_GotFocus(object sender, RoutedEventArgs e)
        {
            selectedList = listText;
            CommandManager.InvalidateRequerySuggested();
        }

        private void Replace_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = listTime?.SelectedIndex > -1 && listText?.SelectedIndex > -1;
        }

        private void Replace_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (listTime.SelectedItems != null && listText.SelectedItems != null)
            {
                var times = listTime.SelectedItems.Cast<Dialogue>();
                var texts = listText.SelectedItems.Cast<Dialogue>();

                Model.ReplaceText(times, texts);
            }
        }

        private void Delete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = selectedList != null && selectedList.SelectedIndex > -1;
        }

        private void Delete_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (selectedList != null && selectedList.SelectedItems != null && Model != null)
            {
                if (selectedList.Name == "listTime")
                {
                    foreach (var d in selectedList.SelectedItems.Cast<Dialogue>().ToArray())
                    {
                        Model.TimeSource.Remove(d);
                    }
                    Model.TimeSource.Renumber();
                }
                else
                {
                    foreach (var d in selectedList.SelectedItems.Cast<Dialogue>().ToArray())
                    {
                        Model.TextSource.Remove(d);
                    }
                    Model.TextSource.Renumber();
                }

                Model.IsEdited = true;
            }
        }

        private void Insert_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = selectedList != null && selectedList.SelectedIndex > -1;
        }

        private void Insert_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (selectedList != null && selectedList.SelectedIndex > -1 && Model != null)
            {
                if (selectedList.Name == "listTime")
                {
                    Model.TimeSource.Insert(selectedList.SelectedIndex, new Dialogue(0));
                    Model.TimeSource.Renumber();
                }
                else
                {
                    Model.TextSource.Insert(selectedList.SelectedIndex, new Dialogue(0));
                    Model.TextSource.Renumber();
                }

                Model.IsEdited = true;
            }
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Model != null && Model.IsEdited;
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Model != null)
            {
                Model.OnSaveExecuted();
            }
        }

        private void SaveAs_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Model != null && Model.IsEdited;
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Model != null)
            {
                Model.OnSaveAsExecuted();
            }
        }
        #endregion
    }
}
