using System.Windows.Input;

namespace BooruViewer.Views
{
    public static class Commands
    {
        private static RoutedUICommand _Search;
        public static RoutedUICommand Search => _Search;

        static Commands()
        {
            InputGestureCollection inputs = new InputGestureCollection();
            inputs.Add(new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl + S"));
            _Search = new RoutedUICommand("Search", "Search", typeof(Commands), inputs);
        }
    }
}
