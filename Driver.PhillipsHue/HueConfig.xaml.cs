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
        public PhillipsHue Instance = null;
        public HueConfig(PhillipsHue instance)
        {
            Instance = instance;
            InitializeComponent();
            IPAddress.Text = Instance?.config?.IPAddress;
            UserName.Text = Instance?.config?.UserName;
            HueKey.Text = Instance?.config?.Key;

            if (Instance?.controlDevices?.Count > 0)
            {
                DevicesFound.Content = Instance?.controlDevices?.Count + " devices found.";
            }
        }

        private async void RequestUserName(object sender, RoutedEventArgs e)
        {
            if (Instance?.config == null)
            {
                Instance.config=new PhillipsHueConfig();
            }

            Instance.config.IPAddress = IPAddress.Text;
            Instance.config.UserName = "";
            Instance.config.Key = "";
            int attempts = 0;
            bool success = false;
            bool toggleWarning=true;
            LinkWarning.Visibility = Visibility.Visible;
            while (!success && attempts < 60)
            {
                try
                {
                    await Instance.Register();
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

            IPAddress.Text = Instance?.config?.IPAddress;
            UserName.Text = Instance?.config?.UserName;
            HueKey.Text = Instance?.config?.Key;

            Instance.Setup();
            Instance.GetDevices(); 

            if (Instance?.controlDevices?.Count > 0)
            {
                DevicesFound.Content = Instance?.controlDevices?.Count + " devices found.";
            }

            Instance?.SetIsDirty(true);
        }
    }
}
