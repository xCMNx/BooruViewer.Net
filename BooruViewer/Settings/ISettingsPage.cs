using System.ComponentModel;

namespace BooruViewer.Settings
{
	public interface ISettingsPage
	{
		bool IsEdited { get; }
		bool CanClose { get; }
		bool Apply();
		void Reset();
	}
}
