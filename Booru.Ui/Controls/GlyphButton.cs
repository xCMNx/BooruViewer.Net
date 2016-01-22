using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Booru.Ui
{
    public class GlyphButton : Button
    {

        static GlyphButton()
        {
           DefaultStyleKeyProperty.OverrideMetadata(typeof(GlyphButton), new FrameworkPropertyMetadata(typeof(GlyphButton)));
        }

        public static readonly DependencyProperty GlyphDataProperty = DependencyProperty.Register(
          nameof(GlyphData),
          typeof(Geometry),
          typeof(GlyphButton)
        );

        public Geometry GlyphData
        {
            get { return (Geometry)GetValue(GlyphDataProperty); }
            set { SetValue(GlyphDataProperty, value); }
        }

        public static readonly DependencyProperty GlyphMarginProperty = DependencyProperty.Register(
          nameof(GlyphMargin),
          typeof(Thickness),
          typeof(GlyphButton)
        );

        public Thickness GlyphMargin
        {
            get { return (Thickness)GetValue(GlyphMarginProperty); }
            set { SetValue(GlyphMarginProperty, value); }
        }
    }
}
