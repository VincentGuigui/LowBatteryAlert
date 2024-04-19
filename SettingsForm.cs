using LowBatteryAlert.Properties;
using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Windows.Forms;
using Windows.ApplicationModel.UserDataAccounts.SystemAccess;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using Windows.Networking.Sockets;
using Windows.Phone.Devices.Power;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Documents;
using Battery = Windows.Devices.Power.Battery;

namespace LowBatteryAlert
{
    public partial class SettingsForm : Form
    {
        Dictionary<string, BatteryDevice> batteryDevices = new Dictionary<string, BatteryDevice>();
        List<Battery> systemBatteries = new List<Battery>();
        List<DeviceInformation> btDevices = new List<DeviceInformation>();
        List<DeviceInformation> btLEDevices = new List<DeviceInformation>();

        public SettingsForm()
        {
            InitializeComponent();
        }

        public async void SettingsForm_Load(object sender, EventArgs e)
        {
            await LoadBatterySettingsAsync();
            timer_Tick(sender, e);
        }

        private async Task GetSytemBatteriesListAsync()
        {
            systemBatteries.Clear();
            btDevices.Clear();
            btLEDevices.Clear();

            // Find batteries 
            string btSelector = BluetoothDevice.GetDeviceSelector();
            string btLESelector = BluetoothLEDevice.GetDeviceSelector();
            string batterySelector = Battery.GetDeviceSelector();
            var batteriesInfo = await DeviceInformation.FindAllAsync(batterySelector);
            var btInfo = await DeviceInformation.FindAllAsync(btSelector);
            var btLEInfo = await DeviceInformation.FindAllAsync(btLESelector);
            if (btLEInfo.Count() > 0)
            {
                foreach (DeviceInformation device in btLEInfo)
                {
                    btLEDevices.Add(device);
                }
            }
            if (btInfo.Count() > 0)
            {
                foreach (DeviceInformation device in btInfo)
                {
                    btDevices.Add(device);
                }
            }
            if (batteriesInfo.Count() > 0)
            {

                foreach (DeviceInformation device in batteriesInfo)
                {
                    var battery = await Battery.FromIdAsync(device.Id);
                    systemBatteries.Add(battery);
                }
            }
        }

        private async Task<double> GetBatteryLevel(BatteryDevice batterySetting)
        {
            switch (batterySetting.Type)
            {
                case BatteryDevice.DeviceType.Bluetooth:
                    return await GetBTBatteryLevel(batterySetting.DeviceId);
                case BatteryDevice.DeviceType.BluetoothLE:
                    return await GetBTLEBatteryLevel(batterySetting.DeviceId);
                default:
                    return GetSystemBatteryLevel(batterySetting.DeviceId);
            }
        }

        private async Task<double> GetBTLEBatteryLevel(string deviceId)
        {
            int level = -1;
            BluetoothLEDevice btLEDevice = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (btLEDevice != null)
            {
                if (btLEDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    ////get UUID of Services
                    var services = btLEDevice.GattServices;
                    if (services != null)
                    {
                        foreach (var service in services)
                        {
                            //if there is a service thats same like the Battery Service
                            if (service.Uuid == GattServiceUuids.Battery)
                            {
                                var characteristics = service.GetAllCharacteristics();
                                foreach (var characteristic in characteristics)
                                {
                                    if (characteristic.Uuid == GattCharacteristicUuids.BatteryLevel)
                                    {
                                        GattReadResult result = await characteristic.ReadValueAsync();
                                        if (result.Status == GattCommunicationStatus.Success)
                                        {
                                            var reader = DataReader.FromBuffer(result.Value);
                                            byte[] input = new byte[reader.UnconsumedBufferLength];
                                            reader.ReadBytes(input);
                                            level = input[0];
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    btLEDevice.Dispose();
                }
            }
            return level;
        }

        private async Task<double> GetBTBatteryLevel(string deviceId)
        {
            int level = -1;
            BluetoothDevice btDevice = await BluetoothDevice.FromIdAsync(deviceId);
            if (btDevice != null)
            {
                //if (btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    RfcommDeviceServicesResult rfcommResult = await btDevice.GetRfcommServicesAsync();
                    if (rfcommResult.Services.Count == 0)
                    {
                        Debug.WriteLine("No services found");
                    }
                    if (rfcommResult.Services.Count > 0)
                    {
                        for (int i = 0; i < rfcommResult.Services.Count; i++)
                        {
                            Debug.WriteLine(String.Format("{0}. {1}", i, rfcommResult.Services[i].ServiceId.Uuid));
                        }
                        int selectedService = 0;

                        try
                        {
                            StreamSocket socket = new StreamSocket();
                            await socket.ConnectAsync(rfcommResult.Services[selectedService].ConnectionHostName, rfcommResult.Services[selectedService].ConnectionServiceName);
                            Debug.WriteLine("Connected to service: " + rfcommResult.Services[selectedService].ServiceId.Uuid);

                            CancellationTokenSource source = new CancellationTokenSource();
                            CancellationToken cancelToken = source.Token;
                            Task listenOnChannel = new TaskFactory().StartNew(async () =>
                            {
                                while (true)
                                {
                                    if (cancelToken.IsCancellationRequested)
                                    {
                                        break;
                                    }
                                    //read data from the socket
                                    DataReader reader = new DataReader(socket.InputStream);
                                    reader.InputStreamOptions = InputStreamOptions.Partial;
                                    await reader.LoadAsync(1);
                                    byte[] input = new byte[1];
                                    reader.ReadBytes(input);
                                    level = input[0];
                                    break;
                                }
                            }, cancelToken);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Could not connect to service " + e.Message);
                        }
                    }
                }
                btDevice.Dispose();
            }
            return level;
        }

        private double GetSystemBatteryLevel(Battery battery)
        {
            var report = battery.GetReport();
            var max = Convert.ToDouble(report.FullChargeCapacityInMilliwattHours);
            var level = Convert.ToDouble(report.RemainingCapacityInMilliwattHours);
            return (level / max) * 100d;
        }

        private double GetSystemBatteryLevel(string deviceId)
        {
            var battery = systemBatteries.SingleOrDefault(b => b.DeviceId == deviceId);
            return GetSystemBatteryLevel(battery);
        }

        private async Task LoadBatterySettingsAsync()
        {
            if (!Properties.Settings.Default.Upgraded)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.Upgraded = true;
                Properties.Settings.Default.Save();
            }
            lstBatteries.Items.Clear();
            batteryDevices.Clear();
            await GetSytemBatteriesListAsync();
            if (systemBatteries.Count == 0 && btDevices.Count == 0 && btLEDevices.Count == 0)
            {
                lstBatteries.Items.Add("No batteries found");
                lstBatteries.Enabled = false;
                numAlertLevel.Enabled = false;
                return;
            }
            else
            {
                lstBatteries.Enabled = true;
                numAlertLevel.Enabled = true;
                var deserializedAlerts = new Dictionary<string, BatteryDevice>();
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Alerts))
                {
                    foreach (var alert in Properties.Settings.Default.Alerts.Split("|||"))
                    {
                        if (!string.IsNullOrEmpty(alert))
                        {
                            var setting = alert.Split(":::");
                            if (setting.Length < 2) break;
                            var batteryDevice = new BatteryDevice()
                            {
                                DeviceId = setting[0],
                                Level = Convert.ToInt32(setting[1])
                            };
                            if (setting.Length > 2)
                            {
                                batteryDevice.Type = setting[2] switch
                                {
                                    "bt" => BatteryDevice.DeviceType.Bluetooth,
                                    "btle" => BatteryDevice.DeviceType.BluetoothLE,
                                    _ => BatteryDevice.DeviceType.SystemBattery,
                                };
                            }
                            batteryDevice.DeviceName = await GetDeviceName(batteryDevice);
                            deserializedAlerts.Add(batteryDevice.DeviceName, batteryDevice);
                        }
                    }
                }

                foreach (var battery in systemBatteries.OrderBy(b => b.DeviceId))
                {
                    AddDefaultAlert(battery.DeviceId, battery.DeviceId, BatteryDevice.DeviceType.SystemBattery, deserializedAlerts);
                }
                foreach (var device in btLEDevices.OrderBy(d => d.Name))
                {
                    AddDefaultAlert(device.Id, device.Name, BatteryDevice.DeviceType.BluetoothLE, deserializedAlerts);
                }
                foreach (var device in btDevices.OrderBy(d => d.Name))
                {
                    AddDefaultAlert(device.Id, device.Name, BatteryDevice.DeviceType.Bluetooth, deserializedAlerts);
                }
                lstBatteries.SelectedIndex = 0;
                chAutoLaunch.Checked = IsAutoLaunchStartup();
            }
        }

        private async Task<string> GetDeviceName(BatteryDevice batteryDevice)
        {
            switch (batteryDevice.Type)
            {
                case BatteryDevice.DeviceType.Bluetooth:
                case BatteryDevice.DeviceType.BluetoothLE:
                    return (await DeviceInformation.CreateFromIdAsync(batteryDevice.DeviceId)).Name;
                default:
                    return batteryDevice.DeviceId;
            }
        }

        private void AddDefaultAlert(string deviceId, string deviceName, BatteryDevice.DeviceType type, Dictionary<string, BatteryDevice> deserializedAlerts)
        {
            var batterySetting = new BatteryDevice()
            {
                DeviceName = deviceName,
                DeviceId = deviceId,
                Type = type
            };
            lstBatteries.Items.Add(batterySetting);
            if (deserializedAlerts.ContainsKey(deviceId))
            {
                batteryDevices.Add(deviceId, deserializedAlerts[deviceId]);
            }
            else
            {
                batteryDevices.Add(deviceId, batterySetting);
            }
        }

        private void SaveBatteryLevelSettings()
        {
            string serializedAlerts = batteryDevices.Aggregate("",
                (acc, next) => acc + next.Key + ":::" + next.Value.Level + "|||" + next.Value.Type + "|||");
            Properties.Settings.Default.Alerts = serializedAlerts;
            Properties.Settings.Default.Save();
            SetAutoLaunchStartup();
            timer_Tick(this, new EventArgs());
        }

        private void SetAutoLaunchStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (chAutoLaunch.Checked)
                rk.SetValue(Application.ProductName, Process.GetCurrentProcess().MainModule.FileName);
            else
                rk.DeleteValue(Application.ProductName, false);
        }

        private bool IsAutoLaunchStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return rk.GetValue(Application.ProductName) != null;
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                SaveBatteryLevelSettings();
            }
            else
                _ = LoadBatterySettingsAsync();
            e.Cancel = true;
            this.Hide();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
        }

        private async void timer_Tick(object sender, EventArgs e)
        {
            bool hasAlert = false;
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            string batteriesLevel = "";
            int i = 0;
            foreach (var battery in systemBatteries.OrderBy(b => b.DeviceId))
            {
                var level = GetSystemBatteryLevel(battery.DeviceId);
                if (systemBatteries.Count > 1)
                    batteriesLevel += $"Battery {i + 1}: {level.ToString("F0")}%\n";
                else
                    batteriesLevel = $"{level.ToString("F0")}%\n";
                hasAlert |= CheckBatteryLevel(battery.DeviceId, level, powerStatus);
                i++;
            }
            foreach (var device in btLEDevices.OrderBy(d => d.Name))
            {
                var level = await GetBTLEBatteryLevel(device.Id);
                batteriesLevel += $"{device.Name}: {level.ToString("F0")}%\n";
                hasAlert |= CheckBatteryLevel(device.Id, level);
            }
            foreach (var device in btDevices.OrderBy(d => d.Name))
            {
                var level = await GetBTBatteryLevel(device.Id);
                batteriesLevel += $"{device.Name}: {level.ToString("F0")}%\n";
                hasAlert |= CheckBatteryLevel(device.Id, level);
            }
            notifyIcon.BalloonTipText = Application.ProductName + ":\n" + batteriesLevel;
            notifyIcon.Text = notifyIcon.BalloonTipText.Length > 60 ? notifyIcon.BalloonTipText.Substring(0, 60) + "..." : notifyIcon.BalloonTipText;
            if (!hasAlert)
            {
                float globalBatteryLevel = powerStatus.BatteryLifePercent;
                if (globalBatteryLevel < .15)
                    notifyIcon.Icon = Resources.LowBatteryAlert_red;
                if (globalBatteryLevel < .35)
                    notifyIcon.Icon = Resources.LowBatteryAlert_25;
                else if (globalBatteryLevel < .65)
                    notifyIcon.Icon = Resources.LowBatteryAlert_50;
                else if (globalBatteryLevel < .85)
                    notifyIcon.Icon = Resources.LowBatteryAlert_75;
                else
                    notifyIcon.Icon = Resources.LowBatteryAlert_100;
            }
        }

        private bool CheckBatteryLevel(string deviceId, double level, PowerStatus? powerStatus = null)
        {
            if (level >= 0 && level <= batteryDevices[deviceId].Level)
            {
                notifyIcon.Icon = Resources.LowBatteryAlert_red;

                if (powerStatus != null && !powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging))
                {
                    string message;
                    if (batteryDevices[deviceId].Type == BatteryDevice.DeviceType.SystemBattery)
                        message = $"Battery level is {level.ToString("F0")}%";
                    else
                        message = $"{batteryDevices[deviceId].DeviceName} battery level is {level.ToString("F0")}%";
                    notifyIcon.ShowBalloonTip(30000, "Low Battery Alert", message, ToolTipIcon.Warning);
                }
                return true;
            }
            return false;
        }

        private async void lstBatteries_SelectedIndexChanged(object sender, EventArgs e)
        {
            var batterySetting = (BatteryDevice)lstBatteries.SelectedItem;

            lblCurrentLevel.Text = (await GetBatteryLevel(batterySetting)).ToString("F2") + "%";
            if (lstBatteries.SelectedItem != null)
                numAlertLevel.Value = (decimal)batteryDevices[batterySetting.DeviceId].Level;
        }

        private void numAlertLevel_ValueChanged(object sender, EventArgs e)
        {
            if (lstBatteries.SelectedItem != null)
                batteryDevices[((BatteryDevice)lstBatteries.SelectedItem).DeviceId].Level = (int)numAlertLevel.Value;
        }

        private void contextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == toolStripMenuItemSettings)
                this.Show();
            else if (e.ClickedItem == toolStripMenuItemClose)
            {
                notifyIcon.Visible = false;
                timer.Stop();
                this.Close();
                Environment.Exit(0);
            }
        }
    }
}