using Ognia.Frontend.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ognia.Frontend.Views.RightPane
{
    public partial class TrackDetailsView : UserControl
    {
        public TrackDetailsView()
        {
            InitializeComponent();
        }

        private void Thumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not TrackDetailsViewModel vm) return;

            var url = vm.TrackSourceUrl;
            if (string.IsNullOrWhiteSpace(url)) return;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}