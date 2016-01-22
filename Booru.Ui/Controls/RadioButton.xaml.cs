using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Booru.Ui
{
    /// <summary>
    /// Логика взаимодействия для RadioButton.xaml
    /// </summary>
    public partial class RadioButton : System.Windows.Controls.RadioButton, INotifyPropertyChanged
    {
        public RadioButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty GlyphDataProperty = DependencyProperty.Register(
          nameof(GlyphData),
          typeof(Geometry),
          typeof(RadioButton)
        );

        public Geometry GlyphData
        {
            get { return (Geometry)GetValue(GlyphDataProperty); }
            set { SetValue(GlyphDataProperty, value); }
        }

        public static readonly DependencyProperty GlyphMarginProperty = DependencyProperty.Register(
          nameof(GlyphMargin),
          typeof(Thickness),
          typeof(RadioButton)
        );

        public Thickness GlyphMargin
        {
            get { return (Thickness)GetValue(GlyphMarginProperty); }
            set { SetValue(GlyphMarginProperty, value); }
        }

        //public static readonly DependencyProperty GlyphMarginProperty = Path.MarginProperty.AddOwner(typeof(RadioButton));

        //public Thickness GlyphMargin
        //{
        //    get { return (Thickness)GetValue(GlyphMarginProperty); }
        //    set { SetValue(GlyphMarginProperty, value); }
        //}

        public static readonly DependencyProperty GlyphSizeProperty = DependencyProperty.Register(
          "GlyphSize",
          typeof(GridLength),
          typeof(RadioButton),
          new PropertyMetadata(new GridLength(10), OnGlyphSizeChanged)
        );

        private static void OnGlyphSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as RadioButton).NotifyPropertyChanged("GlyphSize");
        }

        public GridLength GlyphSize
        {
            get
            {
                return (GridLength)this.GetValue(GlyphSizeProperty);
            }
            set
            {
                this.SetValue(GlyphSizeProperty, value);
                NotifyPropertyChanged(nameof(GlyphSize));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyPropertiesChanged(String[] propertyNames)
        {
            if (PropertyChanged != null)
            {
                foreach (var prop in propertyNames)
                    PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
