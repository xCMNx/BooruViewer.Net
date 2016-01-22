using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;

namespace Booru.Ui
{
    public class NumericUpDown : TextBox
    {
        static NumericUpDown()
        {
            InitializeCommands();
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        }

        public NumericUpDown()
            : base()
        {
            updateValueString();
        }

        protected override void OnPreviewTextInput(System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!Dirty)
                e.Handled = !AreAllValidNumericChars(e.Text);
            base.OnPreviewTextInput(e);
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (!Dirty)
            {
                decimal val = 0;
                if (decimal.TryParse(Text, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out val))
                {
                    Dirty = true;
                    Value = val;
                    Dirty = false;
                }
            }
            base.OnTextChanged(e);
        }

        private bool AreAllValidNumericChars(string str)
        {
            foreach (char c in str)
                if (!Char.IsNumber(c) && c != Sep) return false;
            return true;
        }

        private void updateValueString()
        {
            if (!Dirty)
            {
                Dirty = true;
                //Text = this.Value.ToString("f", _numberFormatInfo);
                Text = this.Value.ToString(NumericFormat, CultureInfo.CurrentCulture.NumberFormat);
                Dirty = false;
            }
        }
        private bool Dirty = false;
        private Char Sep = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
        //private NumberFormatInfo _numberFormatInfo = new NumberFormatInfo() { NumberDecimalDigits = DefaultDecimalPlaces };
        private NumberFormatInfo _numberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;

        #region Properties
        #region ButtonsWidth

        public static readonly DependencyProperty ButtonsWidthProperty = DependencyProperty.Register(
                nameof(ButtonsWidth),
                typeof(GridLength),
                typeof(NumericUpDown),
                new PropertyMetadata(new GridLength(20f), null)
            );

        public GridLength ButtonsWidth
        {
            get { return (GridLength)GetValue(ButtonsWidthProperty); }
            set { SetValue(ButtonsWidthProperty, value); }
        }

        #endregion
        #region Value

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Identifies the Value dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(decimal),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(DefaultValue,
                new PropertyChangedCallback(OnValueChanged),
                new CoerceValueCallback(CoerceValue)
            )
        );

        private static void OnValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            NumericUpDown control = (NumericUpDown)obj;
            decimal oldValue = (decimal)args.OldValue;
            decimal newValue = (decimal)args.NewValue;
            RoutedPropertyChangedEventArgs<decimal> e = new RoutedPropertyChangedEventArgs<decimal>(oldValue, newValue, ValueChangedEvent);
            control.OnValueChanged(e);
            control.updateValueString();
        }

        /// <summary>
        /// Raises the ValueChanged event.
        /// </summary>
        /// <param name="args">Arguments associated with the ValueChanged event.</param>
        protected virtual void OnValueChanged(RoutedPropertyChangedEventArgs<decimal> args)
        {
            RaiseEvent(args);
        }

        private static object CoerceValue(DependencyObject element, object value)
        {
            decimal newValue = (decimal)value;
            NumericUpDown control = (NumericUpDown)element;
            newValue = Math.Max(control.Minimum, Math.Min(control.Maximum, newValue));
            //newValue = Decimal.Round(newValue, control.DecimalPlaces);
            return newValue;
        }

        #endregion
        #region Minimum

        public decimal Minimum
        {
            get { return (decimal)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            nameof(Minimum),
            typeof(decimal),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(
                DefaultMinValue,
                new PropertyChangedCallback(OnMinimumChanged)
            //,new CoerceValueCallback(CoerceMinimum)
            )
        );

        private static void OnMinimumChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            //element.CoerceValue(MaximumProperty);
            element.CoerceValue(ValueProperty);
        }
        //private static object CoerceMinimum(DependencyObject element, object value)
        //{
        //    decimal minimum = (decimal)value;
        //    NumericUpDown control = (NumericUpDown)element;
        //    return Decimal.Round(minimum, control.DecimalPlaces);
        //}

        #endregion
        #region Maximum

        public decimal Maximum
        {
            get { return (decimal)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            nameof(Maximum),
            typeof(decimal),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(DefaultMaxValue,
                new PropertyChangedCallback(OnMaximumChanged)
            //new CoerceValueCallback(CoerceMaximum)
            )
        );

        private static void OnMaximumChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            element.CoerceValue(ValueProperty);
        }

        //private static object CoerceMaximum(DependencyObject element, object value)
        //{
        //    NumericUpDown control = (NumericUpDown)element;
        //    decimal newMaximum = (decimal)value;
        //    return Decimal.Round(Math.Max(newMaximum, control.Minimum), control.DecimalPlaces);
        //}

        #endregion
        #region Change

        public decimal Change
        {
            get { return (decimal)GetValue(ChangeProperty); }
            set { SetValue(ChangeProperty, value); }
        }

        public static readonly DependencyProperty ChangeProperty = DependencyProperty.Register(
            nameof(Change),
            typeof(decimal),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(
                DefaultChange
            //,new PropertyChangedCallback(OnChangeChanged)
            //,new CoerceValueCallback(CoerceChange)
            ),
            new ValidateValueCallback(ValidateChange)
        );

        private static bool ValidateChange(object value) => (decimal)value > 0;

        //private static void OnChangeChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        //{

        //}

        //private static object CoerceChange(DependencyObject element, object value)
        //{
        //    decimal newChange = (decimal)value;
        //    NumericUpDown control = (NumericUpDown)element;
        //    decimal coercedNewChange = Decimal.Round(newChange, control.DecimalPlaces);
        //    //If Change is .1 and DecimalPlaces is changed from 1 to 0, we want Change to go to 1, not 0.
        //    //Put another way, Change should always be rounded to DecimalPlaces, but never smaller than the 
        //    //previous Change
        //    if (coercedNewChange < newChange)
        //        coercedNewChange = smallestForDecimalPlaces(control.DecimalPlaces);
        //    return coercedNewChange;
        //}

        private static decimal smallestForDecimalPlaces(int decimalPlaces)
        {
            if (decimalPlaces < 0)
                throw new ArgumentException(nameof(decimalPlaces));
            decimal d = 1;
            for (int i = 0; i < decimalPlaces; i++)
                d /= 10;
            return d;
        }

        #endregion
        #region NumericFormat
        public string NumericFormat
        {
            get { return (string)GetValue(NumericFormatProperty); }
            set { SetValue(NumericFormatProperty, value); }
        }

        public static readonly DependencyProperty NumericFormatProperty = DependencyProperty.Register(
            nameof(NumericFormat),
            typeof(string),
            typeof(NumericUpDown),
            new FrameworkPropertyMetadata(
                "N",
                new PropertyChangedCallback(NumericFormatChanged)
            ),
            new ValidateValueCallback(ValidateNumericFormat)
        );

        private static void NumericFormatChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            NumericUpDown control = (NumericUpDown)element;
            //control.CoerceValue(ChangeProperty);
            //control.CoerceValue(MinimumProperty);
            //control.CoerceValue(MaximumProperty);
            //control.CoerceValue(ValueProperty);
            //control._numberFormatInfo.NumberDecimalDigits = (int)args.NewValue;
            control.updateValueString();
        }

        private static bool ValidateNumericFormat(object value)
        {
            try
            {
                string.Format((string)value, 0.0001f);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
        #region DecimalPlaces
        //public int DecimalPlaces
        //{
        //    get { return (int)GetValue(DecimalPlacesProperty); }
        //    set { SetValue(DecimalPlacesProperty, value); }
        //}

        //public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
        //    nameof(DecimalPlaces),
        //    typeof(int),
        //    typeof(NumericUpDown),
        //    new FrameworkPropertyMetadata(
        //        DefaultDecimalPlaces,
        //        new PropertyChangedCallback(OnDecimalPlacesChanged)
        //    ), 
        //    new ValidateValueCallback(ValidateDecimalPlaces)
        //);

        //private static void OnDecimalPlacesChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        //{
        //    NumericUpDown control = (NumericUpDown)element;
        //    control.CoerceValue(ChangeProperty);
        //    control.CoerceValue(MinimumProperty);
        //    control.CoerceValue(MaximumProperty);
        //    control.CoerceValue(ValueProperty);
        //    control._numberFormatInfo.NumberDecimalDigits = (int)args.NewValue;
        //    control.updateValueString();
        //}

        //private static bool ValidateDecimalPlaces(object value) => (int)value >= 0;

        #endregion
        #endregion

        #region Events
        /// <summary>
        /// Identifies the ValueChanged routed event.
        /// </summary>
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(ValueChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<decimal>),
            typeof(NumericUpDown)
        );

        /// <summary>
        /// Occurs when the Value property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }
        #endregion

        #region Commands

        public readonly static RoutedCommand IncreaseCommand = new RoutedCommand(nameof(IncreaseCommand), typeof(NumericUpDown));
        public readonly static RoutedCommand DecreaseCommand = new RoutedCommand(nameof(DecreaseCommand), typeof(NumericUpDown));

        private static void InitializeCommands()
        {
            CommandManager.RegisterClassCommandBinding(typeof(NumericUpDown), new CommandBinding(IncreaseCommand, OnIncreaseCommand));
            CommandManager.RegisterClassInputBinding(typeof(NumericUpDown), new InputBinding(IncreaseCommand, new KeyGesture(Key.Up)));

            CommandManager.RegisterClassCommandBinding(typeof(NumericUpDown), new CommandBinding(DecreaseCommand, OnDecreaseCommand));
            CommandManager.RegisterClassInputBinding(typeof(NumericUpDown), new InputBinding(DecreaseCommand, new KeyGesture(Key.Down)));
        }

        private static void OnIncreaseCommand(object sender, ExecutedRoutedEventArgs e) => (sender as NumericUpDown)?.OnIncrease();
        private static void OnDecreaseCommand(object sender, ExecutedRoutedEventArgs e) => (sender as NumericUpDown)?.OnDecrease();

        protected virtual void OnIncrease() => this.Value += Change;
        protected virtual void OnDecrease() => this.Value -= Change;

        #endregion

        private const decimal
            DefaultMinValue = decimal.MinValue,
            DefaultValue = 0,
            DefaultMaxValue = Decimal.MaxValue,
            DefaultChange = 1;
        private const int DefaultDecimalPlaces = 0;
    }
}