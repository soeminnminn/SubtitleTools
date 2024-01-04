using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SubtitleTools.UI.Controls
{
    [TemplatePart(Name = PART_HoursInput, Type = typeof(NumericTextBox))]
    [TemplatePart(Name = PART_MinutesInput, Type = typeof(NumericTextBox))]
    [TemplatePart(Name = PART_SecondsInput, Type = typeof(NumericTextBox))]
    public class TimeSpanTextBox : Control
    {
        #region Variables
        private const string PART_HoursInput = "PART_HoursInput";
        private const string PART_MinutesInput = "PART_MinutesInput";
        private const string PART_SecondsInput = "PART_SecondsInput";

        private NumericTextBox _hoursNumericTextBox;
        private NumericTextBox _minutesNumericTextBox;
        private NumericTextBox _secondsNumericTextBox;

        private IList<NumericTextBox> _numericTextBoxes = Array.Empty<NumericTextBox>();
        #endregion

        #region Dependency Properties
        public TimeSpan? Value
        {
            get { return (TimeSpan?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(TimeSpan?), typeof(TimeSpanTextBox),
            new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is TimeSpanTextBox ctrl)
            {
                ctrl.OnValueChanged((TimeSpan?)e.OldValue, (TimeSpan?)e.NewValue);
                ctrl.RaiseValueChangedEvent((TimeSpan?)e.OldValue, (TimeSpan?)e.NewValue);

                if (ctrl.IsPartsInitialized)
                {
                    ctrl.UpdateUi();
                }
            }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool),
            typeof(TimeSpanTextBox), new PropertyMetadata(false, OnIsReadOnlyChanged));

        private static void OnIsReadOnlyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is TimeSpanTextBox ctrl && ctrl.IsPartsInitialized)
            {
                ctrl._hoursNumericTextBox?.SetCurrentValue(NumericTextBox.IsReadOnlyProperty, (bool)e.NewValue);
                ctrl._minutesNumericTextBox?.SetCurrentValue(NumericTextBox.IsReadOnlyProperty, (bool)e.NewValue);
                ctrl._secondsNumericTextBox?.SetCurrentValue(NumericTextBox.IsReadOnlyProperty, (bool)e.NewValue);
            }
        }
        #endregion

        #region Events
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(nameof(ValueChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<TimeSpan?>), typeof(TimeSpanTextBox));
        public event RoutedPropertyChangedEventHandler<TimeSpan?> ValueChanged
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
        static TimeSpanTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeSpanTextBox), new FrameworkPropertyMetadata(typeof(TimeSpanTextBox)));
        }

        public TimeSpanTextBox()
        {
        }
        #endregion

        #region Properties
        private bool IsPartsInitialized { get; set; }

        private int Hours
        {
            get => (int)(_hoursNumericTextBox?.Value ?? 0);
        }

        private int Minutes
        {
            get => (int)(_minutesNumericTextBox?.Value ?? 0);
        }

        private int Seconds
        {
            get
            {
                decimal seconds = _secondsNumericTextBox?.Value ?? 0;
                return SplitSeconds(seconds).Item1;
            }
        }

        private int Miliseconds
        {
            get
            {
                decimal seconds = _secondsNumericTextBox?.Value ?? 0;
                return SplitSeconds(seconds).Item2;
            }
        }
        #endregion

        #region Methods
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _numericTextBoxes = new List<NumericTextBox>();

            _hoursNumericTextBox = GetTemplateChild(PART_HoursInput) as NumericTextBox;
            if (_hoursNumericTextBox == null)
            {
                throw new InvalidOperationException($"Can't find template part '{PART_HoursInput}'");
            }

            _minutesNumericTextBox = GetTemplateChild(PART_MinutesInput) as NumericTextBox;
            if (_minutesNumericTextBox == null)
            {
                throw new InvalidOperationException($"Can't find template part '{PART_MinutesInput}'");
            }

            _secondsNumericTextBox = GetTemplateChild(PART_SecondsInput) as NumericTextBox;
            if (_secondsNumericTextBox == null)
            {
                throw new InvalidOperationException($"Can't find template part '{PART_SecondsInput}'");
            }

            _hoursNumericTextBox.ValueChanged += OnHoursValueChanged;
            _hoursNumericTextBox.RightBoundReached += OnNumericTextBoxRightBoundReached;
            _numericTextBoxes.Add(_hoursNumericTextBox);

            _minutesNumericTextBox.ValueChanged += OnMinutesValueChanged;
            _minutesNumericTextBox.RightBoundReached += OnNumericTextBoxRightBoundReached;
            _minutesNumericTextBox.LeftBoundReached += OnNumericTextBoxLeftBoundReached;
            _numericTextBoxes.Add(_minutesNumericTextBox);

            _secondsNumericTextBox.ValueChanged += OnSecondsValueChanged;
            _secondsNumericTextBox.LeftBoundReached += OnNumericTextBoxLeftBoundReached;
            _numericTextBoxes.Add(_secondsNumericTextBox);

            IsPartsInitialized = true;
            UpdateUi();
        }

        protected override void OnIsKeyboardFocusedChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!IsPartsInitialized) return;

            if (_hoursNumericTextBox != null)
            {
                _hoursNumericTextBox.SetCurrentValue(FocusableProperty, true);
                Keyboard.Focus(_hoursNumericTextBox);
            }
        }

        protected virtual void OnValueChanged(TimeSpan? oldValue, TimeSpan? newValue)
        { }

        protected virtual void RaiseValueChangedEvent(TimeSpan? oldValue, TimeSpan? newValue)
        {
            RoutedPropertyChangedEventArgs<TimeSpan?> args = new RoutedPropertyChangedEventArgs<TimeSpan?>(oldValue, newValue)
            {
                RoutedEvent = ValueChangedEvent
            };
            RaiseEvent(args);
        }

        private void OnHoursValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal?> e)
        {
            var hours = Hours;
            var value = Value;
            if (value is null)
            {
                SetCurrentValue(ValueProperty, new TimeSpan(0, hours, 0, 0));
            }
            else
            {
                if (value.Value.Hours == hours)
                {
                    return;
                }

                SetCurrentValue(ValueProperty, new TimeSpan(0, hours, Minutes, Seconds, Miliseconds));
            }
        }

        private void OnMinutesValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal?> e)
        {
            var minutes = Minutes;
            var value = Value;
            if (value is null)
            {
                SetCurrentValue(ValueProperty, new TimeSpan(0, 0, minutes, 0));
            }
            else
            {
                if (value.Value.Minutes == minutes)
                {
                    return;
                }

                SetCurrentValue(ValueProperty, new TimeSpan(0, Hours, minutes, Seconds, Miliseconds));
            }
        }

        private void OnSecondsValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal?> e)
        {
            var seconds = SplitSeconds(e.NewValue);
            var value = Value;
            if (value is null)
            {
                SetCurrentValue(ValueProperty, new TimeSpan(0, 0, 0, seconds.Item1, seconds.Item2));
            }
            else
            {
                if (value.Value.Seconds == seconds.Item1 && value.Value.Milliseconds == seconds.Item2)
                {
                    return;
                }

                SetCurrentValue(ValueProperty, new TimeSpan(0, Hours, Minutes, seconds.Item1, seconds.Item2));
            }
        }

        private void OnNumericTextBoxRightBoundReached(object sender, RoutedEventArgs e)
        {
            if (sender is NumericTextBox numericTextBox)
            {
                var currentTextBoxIndex = _numericTextBoxes.IndexOf(numericTextBox);
                var nextTextBox = _numericTextBoxes[currentTextBoxIndex + 1];
                nextTextBox.CaretIndex = 0;
                nextTextBox.Focus();
            }            
        }

        private void OnNumericTextBoxLeftBoundReached(object sender, RoutedEventArgs e)
        {
            if (sender is NumericTextBox numericTextBox)
            {
                var currentTextBoxIndex = _numericTextBoxes.IndexOf(numericTextBox);
                var prevTextBox = _numericTextBoxes[currentTextBoxIndex - 1];
                prevTextBox.CaretIndex = prevTextBox.Text.Length;
                prevTextBox.Focus();
            }
        }

        private void UpdateUi()
        {
            if (!IsPartsInitialized) return;

            var value = Value;

            _hoursNumericTextBox?.SetCurrentValue(NumericTextBox.ValueProperty, (decimal?)(value?.Hours ?? 0));
            _minutesNumericTextBox?.SetCurrentValue(NumericTextBox.ValueProperty, (decimal?)(value?.Minutes ?? 0));

            decimal seconds = ((value?.Seconds ?? 0) * 1000 + (value?.Milliseconds ?? 0)) / 1000.0M;
            _secondsNumericTextBox?.SetCurrentValue(NumericTextBox.ValueProperty, seconds);
        }

        private (int, int) SplitSeconds(decimal? seconds)
        {
            var msVal = Math.Round((seconds ?? 0) * 1000);
            int s = (int)(msVal / 1000) >> 0;
            int ms = (int)(msVal - (s * 1000)) >> 0;
            return (s, ms);
        }
        #endregion
    }
}
