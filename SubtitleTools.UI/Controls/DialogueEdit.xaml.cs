using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SubtitleTools.UI.Controls
{
    /// <summary>
    /// Interaction logic for DialogueEdit.xaml
    /// </summary>
    public partial class DialogueEdit : UserControl
    {
        #region Variables
        public static readonly RoutedCommand BoldCommand = new RoutedCommand("BoldCommand", typeof(DialogueEdit));
        public static readonly RoutedCommand ItalicCommand = new RoutedCommand("ItalicCommand", typeof(DialogueEdit));
        public static readonly RoutedCommand UnderlineCommand = new RoutedCommand("UnderlineCommand", typeof(DialogueEdit));
        public static readonly RoutedCommand BulletListCommand = new RoutedCommand("BulletListCommand", typeof(DialogueEdit));
        public static readonly RoutedCommand AddCharCommand = new RoutedCommand("AddCharCommand", typeof(DialogueEdit));
        #endregion

        #region Constructor
        static DialogueEdit()
        {
            DataContextProperty.OverrideMetadata(typeof(DialogueEdit), new FrameworkPropertyMetadata(OnDataContextChanged, OnCoerceDataContext));
            IsEnabledProperty.OverrideMetadata(typeof(DialogueEdit), new FrameworkPropertyMetadata(OnIsEnabledChanged));

            CommandManager.RegisterClassInputBinding(typeof(DialogueEdit), new InputBinding(BoldCommand, new KeyGesture(Key.B, ModifierKeys.Control)));
            CommandManager.RegisterClassCommandBinding(typeof(DialogueEdit), new CommandBinding(BoldCommand, OnBoldCommandExecuted, OnCommandCanExecute));

            CommandManager.RegisterClassInputBinding(typeof(DialogueEdit), new InputBinding(ItalicCommand, new KeyGesture(Key.I, ModifierKeys.Control)));
            CommandManager.RegisterClassCommandBinding(typeof(DialogueEdit), new CommandBinding(ItalicCommand, OnItalicCommandExecuted, OnCommandCanExecute));

            CommandManager.RegisterClassInputBinding(typeof(DialogueEdit), new InputBinding(UnderlineCommand, new KeyGesture(Key.U, ModifierKeys.Control)));
            CommandManager.RegisterClassCommandBinding(typeof(DialogueEdit), new CommandBinding(UnderlineCommand, OnUnderlineCommandExecuted, OnCommandCanExecute));

            CommandManager.RegisterClassInputBinding(typeof(DialogueEdit), new InputBinding(BulletListCommand, new KeyGesture(Key.L, ModifierKeys.Control)));
            CommandManager.RegisterClassCommandBinding(typeof(DialogueEdit), new CommandBinding(BulletListCommand, OnBulletListCommandExecuted, OnBulletListCommandCanExecute));

            CommandManager.RegisterClassCommandBinding(typeof(DialogueEdit), new CommandBinding(AddCharCommand, OnAddCharCommandExecuted, OnCommandCanExecute));
        }

        public DialogueEdit()
        {
            InitializeComponent();
        }
        #endregion

        #region Properties
        private bool HasText
        {
            get => DialougeInput != null && !string.IsNullOrEmpty(DialougeInput.Text);
        }
        #endregion

        #region Methods
        private static void OnDataContextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is DialogueEdit ctrl)
            {
                var context = e.NewValue as Dialogue;
                ctrl.OnDataContextChanged((Dialogue)e.OldValue, context);
                ctrl.PerformEnableChange(ctrl.IsEnabled && context != null);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private static object OnCoerceDataContext(DependencyObject d, object baseValue)
        {
            if (baseValue == null) return null;
            if (baseValue is Dialogue) return baseValue;
            throw new InvalidOperationException();
        }

        private void OnDataContextChanged(Dialogue oldValue, Dialogue newValue)
        {
        }

        private static void OnIsEnabledChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is DialogueEdit ctrl)
            {
                bool enabled = (bool)e.NewValue;
                ctrl.PerformEnableChange(enabled && ctrl.DataContext != null);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            PerformEnableChange(false);
        }

        private void PerformEnableChange(bool enabled)
        {
            if (!IsEnabled) return;

            StartTimeInput.IsEnabled = enabled;
            EndTimeInput.IsEnabled = enabled;
            DialougeInput.IsEnabled = enabled;
        }

        private static void OnCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is DialogueEdit ctrl)
            {
                e.CanExecute = ctrl.IsEnabled && ctrl.HasText;
            }
        }

        private static void OnBoldCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DialogueEdit ctrl)
            {
                ctrl.OnBoldCommandExecuted();
            }
        }

        private void OnBoldCommandExecuted()
        {
            if (DialougeInput == null) return;

            int start = DialougeInput.SelectionStart;
            int length = DialougeInput.SelectionLength;

            if (length > 0)
            {
                DialougeInput.SelectedText = $"<b>{DialougeInput.SelectedText}</b>";
            }
            else
            {
                DialougeInput.Text = DialougeInput.Text.Insert(start, "<b></b>");
                DialougeInput.SelectionStart = start + 3;
            }
        }

        private static void OnItalicCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DialogueEdit ctrl)
            {
                ctrl.OnItalicCommandExecuted();
            }
        }

        private void OnItalicCommandExecuted()
        {
            if (DialougeInput == null) return;

            int start = DialougeInput.SelectionStart;
            int length = DialougeInput.SelectionLength;

            if (length > 0)
            {
                DialougeInput.SelectedText = $"<i>{DialougeInput.SelectedText}</i>";
            }
            else
            {
                DialougeInput.Text = DialougeInput.Text.Insert(start, "<i></i>");
                DialougeInput.SelectionStart = start + 3;
            }
        }

        private static void OnUnderlineCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DialogueEdit ctrl)
            {
                ctrl.OnUnderlineCommandExecuted();
            }
        }

        private void OnUnderlineCommandExecuted()
        {
            if (DialougeInput == null) return;

            int start = DialougeInput.SelectionStart;
            int length = DialougeInput.SelectionLength;

            if (length > 0)
            {
                DialougeInput.SelectedText = $"<u>{DialougeInput.SelectedText}</u>";
            }
            else
            {
                DialougeInput.Text = DialougeInput.Text.Insert(start, "<u></u>");
                DialougeInput.SelectionStart = start + 3;
            }
        }

        private static void OnAddCharCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DialogueEdit ctrl)
            {
                ctrl.OnAddCharCommandExecuted(e);
            }
        }

        private void OnAddCharCommandExecuted(ExecutedRoutedEventArgs e)
        {
            if (DialougeInput == null) return;

            if (e.Parameter is string s)
            {
                int start = DialougeInput.SelectionStart;
                int length = DialougeInput.SelectionLength;

                if (length > 0 && (s == "♪" || s == "♫"))
                {
                    DialougeInput.SelectedText = $"{s} {DialougeInput.SelectedText} {s}";
                }
                else
                {
                    DialougeInput.Text = DialougeInput.Text.Insert(start + length, s);
                    DialougeInput.SelectionStart = start + s.Length + length;
                }
            }
        }

        private static void OnBulletListCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is DialogueEdit ctrl)
            {
                e.CanExecute = ctrl.IsEnabled && ctrl.HasText;
            }
        }

        private static void OnBulletListCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is DialogueEdit ctrl)
            {
                ctrl.OnBulletListCommandExecuted();
            }
        }

        private void OnBulletListCommandExecuted()
        {
            if (DialougeInput == null) return;

            if (DataContext is Dialogue dialogue)
            {
                var text = dialogue.Text;
                var lines = Utils.SplitLines(text).Select(x => "- " + x).ToArray();
                dialogue.Text = string.Join('\n', lines);
            }
        }
        #endregion
    }
}
