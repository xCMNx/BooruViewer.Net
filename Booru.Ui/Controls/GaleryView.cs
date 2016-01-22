using System.Windows;
using System.Windows.Controls;

namespace Booru.Ui
{
    public class GaleryView : ViewBase
    {
        //public static readonly DependencyProperty ItemHeightProperty = ScrollViewer.HeightProperty.AddOwner(typeof(GaleryView));

        //public double ItemHeight
        //{
        //    get { return (double)GetValue(ItemHeightProperty); }
        //    set { SetValue(ItemHeightProperty, value); }
        //}

        public static readonly DependencyProperty ItemContainerStyleProperty = ItemsControl.ItemContainerStyleProperty.AddOwner(typeof(GaleryView));

        public Style ItemContainerStyle
        {
            get { return (Style)GetValue(ItemContainerStyleProperty); }
            set { SetValue(ItemContainerStyleProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty = ItemsControl.ItemTemplateProperty.AddOwner(typeof(GaleryView));

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ViewTemplateProperty = DependencyProperty.Register(nameof(ViewTemplate), typeof(DataTemplate), typeof(GaleryView));

        public DataTemplate ViewTemplate
        {
            get { return (DataTemplate)GetValue(ViewTemplateProperty); }
            set { SetValue(ViewTemplateProperty, value); }
        }

        protected override object DefaultStyleKey => new ComponentResourceKey(GetType(), "GaleryViewDSK");
    }
}
