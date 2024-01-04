using System;
using System.Windows.Input;
using System.Windows;

namespace SubtitleTools.UI.Controls
{
    // originally came from http://madprops.org/blog/enter-to-tab-as-an-attached-property/
    public static class EnterKeyTraversal
    {
        #region Methods
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool),
            typeof(EnterKeyTraversal), new UIPropertyMetadata(false, OnIsEnabledChanged));

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement frameworkElement)
            {
                if (e.Key != Key.Enter)
                {
                    return;
                }

                e.Handled = true;
                frameworkElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement frameworkElement)
            {
                frameworkElement.Unloaded -= OnUnloaded;
                frameworkElement.PreviewKeyDown -= OnPreviewKeyDown;
            }
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement frameworkElement)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                frameworkElement.Unloaded += OnUnloaded;
                frameworkElement.PreviewKeyDown += OnPreviewKeyDown;
            }
            else
            {
                frameworkElement.Unloaded -= OnUnloaded;
                frameworkElement.PreviewKeyDown -= OnPreviewKeyDown;
            }
        }
        #endregion
    }
}
