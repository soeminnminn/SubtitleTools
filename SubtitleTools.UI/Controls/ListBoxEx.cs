using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SubtitleTools.UI.Controls
{
    public class ListBoxEx : ListBox
    {
        #region Variables
        private ScrollViewer scrollViewer = null;
        #endregion

        #region Constructors
        static ListBoxEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListBoxEx), new FrameworkPropertyMetadata(typeof(ListBoxEx)));
        }

        public ListBoxEx()
            : base()
        { }
        #endregion

        #region Methods
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ListBoxExItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ListBoxExItem;
        }

        public new DependencyObject GetTemplateChild(string childName)
        {
            return base.GetTemplateChild(childName);
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject element) where T : DependencyObject
        {
            if (element == null) yield return (T)Enumerable.Empty<T>();

            int childrenCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(element, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }

        public ScrollViewer GetScrollHost()
        {
            if (scrollViewer == null)
            {
                var childs = FindVisualChildren<ScrollViewer>(this);
                scrollViewer = childs.FirstOrDefault();
            }
            return scrollViewer;
        }
        #endregion
    }

    public class ListBoxExItem : ListBoxItem
    {
        #region Constructors
        //static ListBoxExItem()
        //{
        //    DefaultStyleKeyProperty.OverrideMetadata(typeof(ListBoxExItem), new FrameworkPropertyMetadata(typeof(ListBoxExItem)));
        //}

        public ListBoxExItem()
            : base()
        { }
        #endregion
    }
}
