using System.Windows;

namespace BooruViewer
{
	/// <summary>
	/// Логика взаимодействия для App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			/*
			//Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
			Application.Current.MainWindow = new MainWindow();
			if (new frmSelectDatabase().ShowDialog().Value)
				Application.Current.MainWindow.Show();
			else
				Application.Current.Shutdown();
			//*/
		}
	}
}
