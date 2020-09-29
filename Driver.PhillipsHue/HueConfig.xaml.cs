using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Driver.PhillipsHue
{
    /// <summary>
    /// Interaction logic for HueConfig.xaml
    /// </summary>
    public partial class HueConfig : UserControl
    {
        public HueConfig()
        {
            InitializeComponent();
            IPAddress.Text = PhillipsHue.Instance?.config?.IPAddress;
            UserName.Text = PhillipsHue.Instance?.config?.UserName;
            HueKey.Text = PhillipsHue.Instance?.config?.Key;

            if (PhillipsHue.Instance?.controlDevices?.Count > 0)
            {
                DevicesFound.Content = PhillipsHue.Instance?.controlDevices?.Count + " devices found.";
            }
        }

        private async void RequestUserName(object sender, RoutedEventArgs e)
        {
            PhillipsHue.Instance.config.IPAddress = IPAddress.Text;
            PhillipsHue.Instance.config.UserName = "";
            PhillipsHue.Instance.config.Key = "";
            int attempts = 0;
            bool success = false;
            bool toggleWarning=true;
            LinkWarning.Visibility = Visibility.Visible;
            while (!success && attempts < 60)
            {
                try
                {
                    await PhillipsHue.Instance.Register();
                    success = true;
                }
                catch
                {
                    attempts++;
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    toggleWarning = !toggleWarning;

                    if (toggleWarning)
                    {
                        LinkWarning.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LinkWarning.Visibility = Visibility.Collapsed;
                    }
                }

            }

            LinkWarning.Visibility = Visibility.Collapsed;

            IPAddress.Text = PhillipsHue.Instance?.config?.IPAddress;
            UserName.Text = PhillipsHue.Instance?.config?.UserName;
            HueKey.Text = PhillipsHue.Instance?.config?.Key;

            PhillipsHue.Instance.Setup();
            PhillipsHue.Instance.GetDevices(); 

            if (PhillipsHue.Instance?.controlDevices?.Count > 0)
            {
                DevicesFound.Content = PhillipsHue.Instance?.controlDevices?.Count + " devices found.";
            }

            PhillipsHue.Instance?.SetIsDirty(true);
        }
    }
}
