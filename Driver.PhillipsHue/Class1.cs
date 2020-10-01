using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Driver.PhillipsHue.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Q42.HueApi;
using Q42.HueApi.Models.Bridge;
using Q42.HueApi.Models.Groups;
using Q42.HueApi.Streaming;
using Q42.HueApi.Streaming.Extensions;
using Q42.HueApi.Streaming.Models;
using SimpleLed;
using Light = Driver.PhillipsHue.Models.Light;
using LightCommand = Driver.PhillipsHue.Models.LightCommand;

namespace Driver.PhillipsHue
{
    public class PhillipsHue : ISimpleLedWithConfig
    {
        
        public PhillipsHueConfig config = new PhillipsHueConfig();
        private Timer timer;
        SimpleHueApi api = new SimpleHueApi();
        public PhillipsHue()
        {
        
        }


        private IReadOnlyList<Group> allGroups;
        private RegisterEntertainmentResult registeredEntertainmentResult;
        private StreamingHueClient StreamingClient;
        private bool isReady;
        private bool isConnecting = false;
        public async Task Setup()
        {
            int fd = 0;
            if (controlDevices != null)
            {
                fd = controlDevices.Count;
            }

            if (!string.IsNullOrWhiteSpace(config.IPAddress))
            {
                isConnecting = true;
                LocalHueClient localClient = new LocalHueClient(config.IPAddress);


                Console.WriteLine("Registering");
                if (string.IsNullOrWhiteSpace(config.UserName) || string.IsNullOrWhiteSpace(config.Key))
                {
                    registeredEntertainmentResult = await localClient.RegisterAsync("SLHueDriver", "SimpleLed", true);
                    config.UserName = registeredEntertainmentResult.Username;
                    config.Key = registeredEntertainmentResult.StreamingClientKey;
                    config.DataIsDirty = true;
                }

                Console.WriteLine("Registered");


                StreamingClient = new StreamingHueClient(config.IPAddress, config.UserName, config.Key);
                allGroups = await StreamingClient.LocalHueClient.GetEntertainmentGroups();
                controlDevices = new List<ControlDevice>();
                Debug.WriteLine("Setting up devices");
                foreach (Group allGroup in allGroups)
                {
                    Debug.WriteLine("Working on " + allGroup.Name);

                    var dev = (new PhillipsHueControlDevice
                    {
                        DeviceType = DeviceTypes.Bulb,
                        Driver = this,
                        LEDs = new ControlDevice.LedUnit[allGroup.Lights.Count],
                        Name = allGroup.Name,
                        StreamingGroup = new StreamingGroup(allGroup.Locations),
                        AllGroupId = allGroup.Id
                    });

                    Debug.WriteLine("connecting...");
                    
                    CancellationToken derp = new CancellationToken();
                    
                    for (int i = 0; i < dev.LEDs.Length; i++)
                    {
                        dev.LEDs[i] = new ControlDevice.LedUnit
                        {
                            LEDName = "Bulb " + allGroup.Lights[i],
                            Data = new ControlDevice.LEDData()
                            {
                                LEDNumber = int.Parse(allGroup.Lights[i])
                            },
                            Color = new LEDColor(0, 0, 0)
                        };
                    }

                    Debug.WriteLine("adding device");

                    controlDevices.Add(dev);
                }

                isReady = true;
                isConnecting = false;
                
                if (fd != controlDevices.Count)
                {
                    config.DataIsDirty = true;
                    FireDeviceRescanRequired();
                }

                Debug.WriteLine("All done");




    
            }
        }

        public async Task Register()
        {
            if (!string.IsNullOrWhiteSpace(config.IPAddress))
            {
                LocalHueClient localClient = new LocalHueClient(config.IPAddress);
                Console.WriteLine("Registering");
                if (string.IsNullOrWhiteSpace(config.UserName) || string.IsNullOrWhiteSpace(config.Key))
                {
                    registeredEntertainmentResult = await localClient.RegisterAsync("SLHueDriver", "SimpleLed", true);
                    config.UserName = registeredEntertainmentResult.Username;
                    config.Key = registeredEntertainmentResult.StreamingClientKey;
                    config.DataIsDirty = true;
                }

                Console.WriteLine("Registered");
            }
        }

        public List<ControlDevice> controlDevices = new List<ControlDevice>();

        private bool isWriting = false;


        public void Dispose()
        {

        }

        public event EventHandler DeviceRescanRequired;

        public void FireDeviceRescanRequired()
        {
            OnFireDeviceRescanRequired(new EventArgs());
        }

        void OnFireDeviceRescanRequired(EventArgs e)
        {
            DeviceRescanRequired?.Invoke(this, e);
        }

        public void Configure(DriverDetails driverDetails)
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;


            //  timer = new Timer(TimerCallback, null, 0, 150);

            Setup();
        }

        public List<ControlDevice> GetDevices()
        {
            if (!string.IsNullOrWhiteSpace(config.UserName) && !string.IsNullOrWhiteSpace(config.Key) &&
                !string.IsNullOrWhiteSpace(config.IPAddress))
            {
                int ct = 0;
                while (!isReady && ct < 10)
                {
                    Thread.Sleep(1000);
                    Debug.WriteLine("Waiting...");
                    if (!isConnecting)
                    {
                        Setup();
                    }

                    ct++;
                }
            }

            return controlDevices;
        }

        public void Push(ControlDevice controlDevice)
        {
            if (isWriting)
            {
                return;
            }


            isWriting = true;
            try
            {
                PhillipsHueControlDevice pcd = (PhillipsHueControlDevice)controlDevice;

                var entLayer = pcd.StreamingGroup.GetNewLayer(isBaseLayer: true);
                int ct = 0;


                foreach (EntertainmentLight entertainmentLight in entLayer)
                {
                    var led = pcd.LEDs[ct];
                    Q42.HueApi.ColorConverters.RGBColor thisCol = new Q42.HueApi.ColorConverters.RGBColor
                    {
                        R = led.Color.Red / 255f,
                        G = led.Color.Green / 255f,
                        B = led.Color.Blue / 255f,
                    };

                    entertainmentLight.SetState(CancellationToken.None, thisCol, 1.0, TimeSpan.FromMilliseconds(0));
                    ct++;
                }

                if (!pcd.HasConnected && !isConnecting)
                {
                    isConnecting = true;
                    try
                    {
                        Debug.WriteLine("Connecting");
                        if (StreamingClient == null)
                        {
                            StreamingClient = new StreamingHueClient(config.IPAddress, config.UserName, config.Key);
                        }

                        StreamingClient.Connect(pcd.AllGroupId, false).Wait();
                        pcd.HasConnected = true;
                        Debug.WriteLine("Connected");

                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                    isConnecting = false;
                }

                StreamingClient.ManualUpdate(pcd.StreamingGroup, true);
            }
            catch
            {
            }

            lastRefresh = DateTime.Now;
            isWriting = false;
        }

        TimeSpan minRefresh = TimeSpan.FromMilliseconds(200);
        DateTime lastRefresh = DateTime.MinValue;
        public void Pull(ControlDevice controlDevice)
        {

        }

        private DriverProperties driverProps;
        public DriverProperties GetProperties()
        {
            if (driverProps == null)
            {

                driverProps = new DriverProperties
                {
                    Author = "Mad Ninja",
                    Blurb = "Simple Driver for HUE bulbs",
                    CurrentVersion = new ReleaseNumber(1, 0, 0, 1004),
                    GitHubLink = "https://github.com/SimpleLed/Driver.PhillipsHue",
                    Id = Guid.Parse("14e1f193-5e17-4e56-82ce-6a3f8f282020"),
                    IsPublicRelease = false,
                    IsSource = false,
                    SupportsPull = false,
                    SupportsPush = true
                };
            }

            return driverProps;
        }

        public T GetConfig<T>() where T : SLSConfigData
        {
            return config as T;
        }

        public void PutConfig<T>(T config) where T : SLSConfigData
        {
            this.config = config as PhillipsHueConfig;
        }

        public string Name()
        {
            return "PhillipsHue";
        }

        public class PhillipsHueControlDevice : ControlDevice
        {
            internal bool HasConnected { get; set; } = false;
            public Light Light { get; set; }
            public StreamingGroup StreamingGroup { get; set; }
            public string AllGroupId { get; set; }
        }

        public UserControl GetCustomConfig(ControlDevice controlDevice)
        {
            return new HueConfig(this);
        }

        public bool GetIsDirty()
        {
            if (config == null) return false;
            return config.DataIsDirty;
        }

        public void SetIsDirty(bool val)
        {
            config.DataIsDirty = true;
        }
    }

    public class PhillipsHueConfig : SLSConfigData
    {
        public string IPAddress { get; set; }
        public string UserName { get; set; }
        public string Key { get; set; }
    }
}
