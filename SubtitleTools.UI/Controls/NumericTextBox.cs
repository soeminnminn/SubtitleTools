using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace SubtitleTools.UI.Controls
{
    public class NumericTextBox : TextBox
    {
        #region Variables
        private const string MinusCharacter = "-";
        private const string PeriodCharacter = ".";
        private const string CommaCharacter = ",";

        private static readonly HashSet<Key> AllowedKeys = new()
        {
            Key.Back,
            Key.CapsLock,
            Key.LeftCtrl,
            Key.RightCtrl,
            Key.Down,
            Key.End,
            Key.Enter,
            Key.Escape,
            Key.Home,
            Key.Insert,
            Key.Left,
            Key.PageDown,
            Key.PageUp,
            Key.Right,
            Key.LeftShift,
            Key.RightShift,
            Key.Tab,
            Key.Up,
            Key.Delete
        };

        private readonly MouseButtonEventHandler _selectivelyIgnoreMouseButtonDelegate;
        private readonly RoutedEventHandler _selectAllTextDelegate;

        private bool _textChangingIsInProgress = true;
        private bool _suspendTextChanged;
        #endregion

        #region Dependency Properties
        public string NullString
        {
            get { return (string)GetValue(NullStringProperty); }
            set { SetValue(NullStringProperty, value); }
        }

        public static readonly DependencyProperty NullStringProperty = DependencyProperty.Register(
            nameof(NullString), typeof(string), typeof(NumericTextBox), new PropertyMetadata(default(string)));


        public CultureInfo CultureInfo
        {
            get { return (CultureInfo)GetValue(CultureInfoProperty); }
            set { SetValue(CultureInfoProperty, value); }
        }

        public static readonly DependencyProperty CultureInfoProperty = DependencyProperty.Register(
            nameof(CultureInfo), typeof(CultureInfo), typeof(NumericTextBox), new PropertyMetadata(CultureInfo.CurrentCulture));


        public bool IsChangeValueByUpDownKeyEnabled
        {
            get { return (bool)GetValue(IsChangeValueByUpDownKeyEnabledProperty); }
            set { SetValue(IsChangeValueByUpDownKeyEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsChangeValueByUpDownKeyEnabledProperty = DependencyProperty.Register(
            nameof(IsChangeValueByUpDownKeyEnabled), typeof(bool), typeof(NumericTextBox), new PropertyMetadata(true));


        public bool IsNullValueAllowed
        {
            get { return (bool)GetValue(IsNullValueAllowedProperty); }
            set { SetValue(IsNullValueAllowedProperty, value); }
        }

        public static readonly DependencyProperty IsNullValueAllowedProperty = DependencyProperty.Register(nameof(IsNullValueAllowed), typeof(bool),
            typeof(NumericTextBox), new PropertyMetadata(true, OnIsNullValueAllowedChanged));

        private static void OnIsNullValueAllowedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is NumericTextBox ctrl)
            {
                ctrl.OnIsNullValueAllowedChanged((bool)e.OldValue, (bool)e.NewValue);
            }
        }

        public bool IsNegativeAllowed
        {
            get { return (bool)GetValue(IsNegativeAllowedProperty); }
            set { SetValue(IsNegativeAllowedProperty, value); }
        }

        public static readonly DependencyProperty IsNegativeAllowedProperty = DependencyProperty.Register(nameof(IsNegativeAllowed), typeof(bool),
            typeof(NumericTextBox), new PropertyMetadata(false, OnIsNegativeAllowedChanged));

        private static void OnIsNegativeAllowedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is NumericTextBox ctrl)
            {
                ctrl.OnIsNegativeAllowedChanged((bool)e.OldValue, (bool)e.NewValue);
            }
        }

        public bool IsDecimalAllowed
        {
            get { return (bool)GetValue(IsDecimalAllowedProperty); }
            set { SetValue(IsDecimalAllowedProperty, value); }
        }

        public static readonly DependencyProperty IsDecimalAllowedProperty = DependencyProperty.Register(nameof(IsDecimalAllowed), typeof(bool),
            typeof(NumericTextBox), new PropertyMetadata(false, OnIsDecimalAllowedChanged));

        private static void OnIsDecimalAllowedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is NumericTextBox ctrl)
            {
                ctrl.OnIsDecimalAllowedChanged((bool)e.OldValue, (bool)e.NewValue);
            }
        }

        public decimal MinValue
        {
            get { return (decimal)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(nameof(MinValue), typeof(decimal),
            typeof(NumericTextBox), new UIPropertyMetadata(decimal.MinValue, OnMinValueChanged));

        private static void OnMinValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is NumericTextBox ctrl)
            {
                ctrl.OnMinValueChanged((decimal)e.OldValue, (decimal)e.NewValue);
            }
        }

        public decimal MaxValue
        {
            get { return (decimal)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(nameof(MaxValue), typeof(decimal),
            typeof(NumericTextBox), new UIPropertyMetadata(decimal.MaxValue, OnMaxValueChanged));

        private static void OnMaxValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is NumericTextBox ctrl)
            {
                ctrl.OnMaxValueChanged((decimal)e.OldValue, (decimal)e.NewValue);
            }
        }

        public string Format
        {
            get { return (string)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(nameof(Format), typeof(string),
            typeof(NumericTextBox), new UIPropertyMetadata("F0", OnFormatChanged));

        private static void OnFormatChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is NumericTextBox ctrl)
            {
                ctrl.OnFormatChanged((string)e.OldValue, (string)e.NewValue);
            }
        }


        public decimal Increment
        {
            get
            {
                return (decimal)GetValue(IncrementProperty);
            }
            set
            {
                SetValue(IncrementProperty, value);
            }
        }

        public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(nameof(Increment), typeof(decimal),
            typeof(NumericTextBox), new PropertyMetadata(1M, OnIncrementChanged, OnCoerceIncrement));

        private static void OnIncrementChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is NumericTextBox ctrl)
            {
                ctrl.OnIncrementChanged((decimal)e.OldValue, (decimal)e.NewValue);
            }
        }

        private static object OnCoerceIncrement(DependencyObject d, object baseValue)
        {
            if (baseValue == null) return 1;
            return baseValue;
        }


        public decimal? Value
        {
            get { return (decimal?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(decimal?),
            typeof(NumericTextBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is NumericTextBox ctrl)
            {
                ctrl.OnValueChanged((decimal?)e.OldValue, (decimal?)e.NewValue);
                ctrl.RaiseValueChangedEvent((decimal?)e.OldValue, (decimal?)e.NewValue);
            }
        }
        #endregion

        #region Events
        public static readonly RoutedEvent RightBoundReachedEvent = EventManager.RegisterRoutedEvent(nameof(RightBoundReached), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(NumericTextBox));
        public event RoutedEventHandler RightBoundReached
        {
            add
            {
                AddHandler(RightBoundReachedEvent, value);
            }
            remove
            {
                RemoveHandler(RightBoundReachedEvent, value);
            }
        }

        public static readonly RoutedEvent LeftBoundReachedEvent = EventManager.RegisterRoutedEvent(nameof(LeftBoundReached), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(NumericTextBox));
        public event RoutedEventHandler LeftBoundReached
        {
            add
            {
                AddHandler(LeftBoundReachedEvent, value);
            }
            remove
            {
                RemoveHandler(LeftBoundReachedEvent, value);
            }
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(nameof(ValueChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<decimal?>), typeof(NumericTextBox));
        public event RoutedPropertyChangedEventHandler<decimal?> ValueChanged
        {
            add
            {
                AddHandler(ValueChangedEvent, value);
            }
            remove
            {
                RemoveHandler(ValueChangedEvent, value);
            }
        }
        #endregion

        #region Constructors
        static NumericTextBox()
        {
            // DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(typeof(NumericTextBox)));

            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(HorizontalAlignment.Right));
            VerticalContentAlignmentProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(VerticalAlignment.Center));
        }

        public NumericTextBox()
        {
            _selectivelyIgnoreMouseButtonDelegate = SelectivelyIgnoreMouseButton;
            _selectAllTextDelegate = SelectAllText;

            HorizontalContentAlignment = HorizontalAlignment.Right;
            VerticalContentAlignment = VerticalAlignment.Center;

            Unloaded += OnUnloaded;
        }
        #endregion

        #region Properties
        private bool AllTextSelected => SelectedText == Text;

        private bool CaretAtStart => CaretIndex == 0;

        private bool CaretAtEnd => CaretIndex == Text.Length;
        #endregion

        #region Methods
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            AddHandler(PreviewMouseLeftButtonDownEvent, _selectivelyIgnoreMouseButtonDelegate, true);
            AddHandler(GotKeyboardFocusEvent, _selectAllTextDelegate, true);
            AddHandler(MouseDoubleClickEvent, _selectAllTextDelegate, true);

            DataObject.AddPastingHandler(this, OnPaste);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            RemoveHandler(PreviewMouseLeftButtonDownEvent, _selectivelyIgnoreMouseButtonDelegate);
            RemoveHandler(GotKeyboardFocusEvent, _selectAllTextDelegate);
            RemoveHandler(MouseDoubleClickEvent, _selectAllTextDelegate);

            DataObject.RemovePastingHandler(this, OnPaste);
        }

        protected virtual void OnMinValueChanged(decimal oldValue, decimal newValue)
        {
            SetCurrentValue(ValueProperty, CoerceValueWithMaxMinBoundaries());
        }

        protected virtual void OnMaxValueChanged(decimal oldValue, decimal newValue)
        {
            SetCurrentValue(ValueProperty, CoerceValueWithMaxMinBoundaries());
        }

        private decimal? CoerceValueWithMaxMinBoundaries()
        {
            var value = Value;
            if (value > MaxValue)
            {
                return MaxValue;
            }

            return value < MinValue ? MinValue : value;
        }

        protected virtual void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!IsDecimalAllowed && !IsDigitsOnly(text))
                {
                    e.CancelCommand();
                }
                else if (!IsNegativeAllowed && text.Contains(MinusCharacter))
                {
                    e.CancelCommand();
                }

                if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out _))
                {
                    return;
                }

                e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        protected virtual void OnIsNullValueAllowedChanged(bool oldValue, bool newValue)
        {
            //EnforceRules();
        }

        protected virtual void OnIsNegativeAllowedChanged(bool oldValue, bool newValue)
        {
            if (IsNegativeAllowed)
            {
                AllowedKeys.Add(Key.OemMinus);
            }
            else
            {
                if (AllowedKeys.Contains(Key.OemMinus))
                {
                    AllowedKeys.Remove(Key.OemMinus);
                }
            }

            // EnforceRules();
        }

        protected virtual void OnIsDecimalAllowedChanged(bool oldValue, bool newValue)
        {
            // EnforceRules();
        }

        protected virtual void OnIncrementChanged(decimal oldValue, decimal newValue)
        {

        }

        private static bool DoesStringValueRequireUpdate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            var update = true;

            // CTL-1000 NumericTextBox behavior doesn't allow some values (e.g. 2.05)
            var separator = Math.Max(text.IndexOf(CommaCharacter), text.IndexOf(PeriodCharacter));
            if (separator >= 0)
            {
                var resetUpdate = true;

                for (var i = separator + 1; i < text.Length; i++)
                {
                    if (text[i] == '0')
                    {
                        continue;
                    }

                    resetUpdate = false;
                    break;
                }

                if (resetUpdate)
                {
                    update = false;
                }
            }

            // CTL-761
            if (string.Equals(text, "-") || string.Equals(text, "-0"))
            {
                // User is typing -0 (would result in 0, which we don't want yet, maybe they are typing -0.5)
                update = false;
            }

            if (text.StartsWith(CommaCharacter) || text.EndsWith(CommaCharacter) ||
                text.StartsWith(PeriodCharacter) || text.EndsWith(PeriodCharacter))
            {
                // User is typing a . or , don't update
                update = false;
            }

            return update;
        }

        private bool IsValidValue(decimal inputValue)
        {
            return inputValue <= MaxValue && inputValue >= MinValue;
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            SetCurrentValue(ValueProperty, ParseValue(Text));

            using (new DisposableToken<NumericTextBox>(this, x => x._suspendTextChanged = true, x => x._suspendTextChanged = false))
            {
                UpdateText();
            }
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            ArgumentNullException.ThrowIfNull(this);

            if (_suspendTextChanged)
            {
                return;
            }

            if (_textChangingIsInProgress && IsKeyboardFocused)
            {
                return;
            }

            using (new DisposableToken<NumericTextBox>(this, x => x._textChangingIsInProgress = false, x => x._textChangingIsInProgress = true))
            {
                UpdateValue();
            }
        }

        internal void UpdateValue()
        {
            var text = Text;

            if (!IsNegativeAllowed && text.StartsWith("-"))
            {
                SetCurrentValue(TextProperty, text.Replace("-", string.Empty));

                return;
            }

            if (DoesStringValueRequireUpdate(text))
            {
                SetCurrentValue(ValueProperty, ParseValue(text));
            }
        }

        private decimal? ParseValue(string text)
        {
            decimal? result = null;

            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    var culture = CultureInfo;

                    var factor = 1d;
                    var percentSymbol = culture.NumberFormat.PercentSymbol;
                    if (text.Contains(percentSymbol))
                    {
                        text = text.Replace(percentSymbol, string.Empty);
                        factor = 1d / 100;
                    }

                    // TODO: Do we want to handle P2, etc (e.g. 50.00%)
                    result = Convert.ToDecimal(text, culture) * (decimal)factor;
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            if (!IsNullValueAllowed && !result.HasValue)
            {
                result = default(decimal);
            }

            return result;
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);

            var text = GetText(e.Text);
            if (text == string.Empty)
            {
                return;
            }

            if (!IsNegativeAllowed && text.StartsWith("-"))
            {
                SetCurrentValue(TextProperty, text.Replace("-", string.Empty));

                return;
            }

            if (!DoesStringValueRequireUpdate(text))
            {
                return;
            }

            if (!decimal.TryParse(text, out var value))
            {
                e.Handled = true;
                return;
            }

            if (!IsValidValue(value))
            {
                e.Handled = true;
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Right && (CaretAtEnd || CaretAtStart && AllTextSelected))
            {
                RaiseRightBoundReachedEvent();
                e.Handled = true;
            }

            if (e.Key == Key.Left && CaretAtStart)
            {
                RaiseLeftBoundReachedEvent();
                e.Handled = true;
            }

            if (IsChangeValueByUpDownKeyEnabled && e.Key == Key.Up && AllTextSelected && !IsReadOnly)
            {
                OnUpDown(Increment);
                e.Handled = true;
            }

            if (IsChangeValueByUpDownKeyEnabled && e.Key == Key.Down && AllTextSelected && !IsReadOnly)
            {
                OnUpDown(-Increment);
                e.Handled = true;
            }

            e.Handled = IsKeyNotAllowed(e);
        }

        private bool IsKeyNotAllowed(KeyEventArgs e)
        {
            var keyValue = GetKeyValue(e);

            var numberDecimalSeparator = GetDecimalSeparator();

            if (keyValue == numberDecimalSeparator && IsDecimalAllowed)
            {
                return Text.Contains(numberDecimalSeparator);
            }

            if (keyValue == MinusCharacter && IsNegativeAllowed)
            {
                return CaretIndex > 0;
            }

            if (AllowedKeys.Contains(e.Key) || IsDigit(e))
            {
                return e.Key == Key.OemMinus && CaretIndex > 0 && IsNegativeAllowed;
            }

            return true;
        }

        private void OnUpDown(decimal increment)
        {
            var value = Value;
            var newValue = value is null ? MinValue : GetNewValue(value.Value, increment);

            var textValue = newValue.ToString(Format, CultureInfo);

            SetCurrentValue(TextProperty, textValue);

            using (new DisposableToken<NumericTextBox>(this, x => x._textChangingIsInProgress = false, x => x._textChangingIsInProgress = true))
            {
                SetCurrentValue(ValueProperty, ParseValue(textValue));
            }

            InvokeDispatcher(SelectAll, 1);
        }

        private decimal GetNewValue(decimal oldValue, decimal increment)
        {
            if (oldValue.Equals(MaxValue) && increment > 0M)
            {
                return MinValue;
            }

            if (oldValue.Equals(MinValue) && increment < 0M)
            {
                return MaxValue;
            }

            return oldValue + increment;
        }

        private string GetText(string inputText)
        {
            var text = new StringBuilder(base.Text);

            var selectedText = SelectedText;
            if (!string.IsNullOrEmpty(selectedText))
            {
                text.Remove(CaretIndex, selectedText.Length);
            }

            text.Insert(CaretIndex, inputText);

            return text.ToString();
        }

        private static void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            var parent = e.OriginalSource as DependencyObject;
            if (parent is null)
            {
                return;
            }

            while (parent is not null and not TextBox)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is TextBox textBox)
            {
                if (textBox.IsFocused || textBox.IsKeyboardFocusWithin)
                {
                    return;
                }
                textBox.Focus();
                e.Handled = true;
            }
        }

        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
                textBox.SelectAll();
        }

        protected virtual void OnValueChanged(decimal? oldValue, decimal? newValue)
        {
            if (_textChangingIsInProgress && IsKeyboardFocused)
            {
                return;
            }

            UpdateText();
        }

        protected virtual void OnFormatChanged(string oldValue, string newValue)
        {
            UpdateText();
        }

        protected virtual void RaiseRightBoundReachedEvent()
        {
            RoutedEventArgs args = new RoutedEventArgs()
            {
                RoutedEvent = RightBoundReachedEvent
            };
            RaiseEvent(args);
        }

        protected virtual void RaiseLeftBoundReachedEvent()
        {
            RoutedEventArgs args = new RoutedEventArgs()
            {
                RoutedEvent = LeftBoundReachedEvent
            };
            RaiseEvent(args);
        }

        protected virtual void RaiseValueChangedEvent(decimal? oldValue, decimal? newValue)
        {
            RoutedPropertyChangedEventArgs<decimal?> args = new RoutedPropertyChangedEventArgs<decimal?>(oldValue, newValue)
            {
                RoutedEvent = ValueChangedEvent
            };
            RaiseEvent(args);
        }

        internal void UpdateText()
        {
            var textValue = Value is null ? NullString : Value.Value.ToString(Format, CultureInfo ?? CultureInfo.CurrentCulture);

            SetCurrentValue(TextProperty, textValue);
        }

        private void EnforceRules()
        {
            var value = Value;
            if (value.HasValue)
            {
                if (!IsNegativeAllowed && value.Value < 0)
                {
                    value = 0;
                }

                if (!IsDecimalAllowed)
                {
                    value = Math.Round(value.Value, 0);
                }
            }
            else
            {
                value = MinValue;
            }

            if (!Equals(value, Value))
            {
                SetCurrentValue(ValueProperty, value);
            }
        }

        private string GetDecimalSeparator()
        {
            var numberDecimalSeparator = CultureInfo.NumberFormat.NumberDecimalSeparator;
            return numberDecimalSeparator;
        }

        private bool IsDigitsOnly(string input)
        {
            return input.All(c => c >= '0' && c <= '9');
        }

        private static bool IsDigit(KeyEventArgs e)
        {
            Key key = e.Key;
            bool isDigit;
            var isShiftKey = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (key is >= Key.D0 and <= Key.D9 && !isShiftKey)
            {
                isDigit = true;
            }
            else
            {
                isDigit = key is >= Key.NumPad0 and <= Key.NumPad9;
            }

            return isDigit;
        }

        private string GetKeyValue(KeyEventArgs e)
        {
            var keyValue = string.Empty;

            switch (e.Key)
            {
                case Key.Decimal:
                    keyValue = GetDecimalSeparator();
                    break;

                case Key.OemMinus:
                case Key.Subtract:
                    keyValue = MinusCharacter;
                    break;

                case Key.OemComma:
                    keyValue = CommaCharacter;
                    break;

                case Key.OemPeriod:
                    keyValue = PeriodCharacter;
                    break;
            }

            return keyValue;
        }

        private void InvokeDispatcher(Action action, int delay)
        {
            if (delay is 0)
            {
                action.Invoke();

                return;
            }

            var timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, delay),
                Tag = action
            };

            timer.Tick += OnDispatcherTimerTick;

            timer.Stop();
            timer.Start();
        }

        private void OnDispatcherTimerTick(object sender, EventArgs e)
        {
            if (sender is not DispatcherTimer timer)
            {
                return;
            }

            timer.Stop();
            timer.Tick -= OnDispatcherTimerTick;

            var action = timer.Tag as Action;
            action?.Invoke();
        }
        #endregion

        #region Nested Types
        private class DisposableToken<T> : IDisposable
        {
            private readonly object _syncRoot = new object();

            private readonly T _instance;
            private readonly Action<T> _dispose;
            private bool _disposing;

            public DisposableToken(T instance, Action<T> initialize, Action<T> dispose)
            {
                _instance = instance;
                _dispose = dispose;

                if (initialize != null)
                {
                    initialize(instance);
                }
            }

            ~DisposableToken()
            {
                Dispose(false);
            }

            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool isDisposing)
            {
                lock (_syncRoot)
                {
                    if (!IsDisposed && !_disposing)
                    {
                        _disposing = true;

                        if (isDisposing)
                        {
                            try
                            {
                                if (_dispose != null)
                                {
                                    _dispose(_instance);
                                }
                            }
                            catch
                            { }
                        }


                        IsDisposed = true;
                        _disposing = false;
                    }
                }
            }
        }
        #endregion
    }
}
