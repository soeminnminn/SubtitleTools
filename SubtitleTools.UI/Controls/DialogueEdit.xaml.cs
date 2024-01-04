using System;
using System.Windows;
using System.Windows.Controls;

namespace SubtitleTools.UI.Controls
{
    /// <summary>
    /// Interaction logic for DialogueEdit.xaml
    /// </summary>
    public partial class DialogueEdit : UserControl
    {
        #region Variables
        #endregion

        #region Dependency Properties
        #endregion

        #region Constructor
        static DialogueEdit()
        {
            DataContextProperty.OverrideMetadata(typeof(DialogueEdit), new FrameworkPropertyMetadata(OnDataContextChanged, OnCoerceDataContext));
            IsEnabledProperty.OverrideMetadata(typeof(DialogueEdit), new FrameworkPropertyMetadata(OnIsEnabledChanged));
        }

        public DialogueEdit()
        {
            InitializeComponent();
        }
        #endregion

        #region Properties
        #endregion

        #region Methods
        private static void OnDataContextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is DialogueEdit ctrl)
            {
                var context = e.NewValue as Dialogue;
                ctrl.OnDataContextChanged((Dialogue)e.OldValue, context);
                ctrl.PerformEnableChange(ctrl.IsEnabled && context != null);
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
        #endregion
    }
}
