using LowBatteryAlert.Properties;
using Microsoft.Win32;
using System.Diagnostics;

namespace LowBatteryAlert
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        public async void SettingsForm_Load(object sender, EventArgs e)
        {
            await LoadBatterySettingsAsync();
            timer_Tick(sender, e);
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
            await BatteryDevice.RefreshBatteriesList();
            if (BatteryDevice.All.Count == 0)
            {
                lstBatteries.Items.Add("No batteries found");
                lstBatteries.Enabled = false;
                numAlerttThreshold.Enabled = false;
                return;
            }
            else
            {
                lstBatteries.Enabled = true;
                lstBatteries.Items.AddRange(BatteryDevice.All.FindAll(b => b.Connected).ToArray());
                numAlerttThreshold.Enabled = true;
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Alerts))
                {
                    foreach (var alert in Properties.Settings.Default.Alerts.Split("|||"))
                    {
                        if (!string.IsNullOrEmpty(alert))
                        {
                            var setting = alert.Split(":::");
                            if (setting.Length < 2) break;
                            string deviceId = setting[0];
                            int threshold = Convert.ToInt32(setting[1]);
                            BatteryDevice.DeviceType type = BatteryDevice.DeviceType.SystemBattery;
                            if (deviceId.ToLower().Contains("bluetoothle"))
                                type = BatteryDevice.DeviceType.BluetoothLE;
                            else if (deviceId.ToLower().Contains("bluetooth"))
                                type = BatteryDevice.DeviceType.Bluetooth;
                            BatteryDevice? device = BatteryDevice.Get(deviceId);
                            if (device != null)
                            {
                                device.Threshold = threshold;
                            }
                            else
                            {
                                var batteryDevice = new BatteryDevice(deviceId, deviceId, type, false, threshold);
                                BatteryDevice.All.Add(batteryDevice);
                            }
                        }
                    }
                }
                lstBatteries.SelectedIndex = 0;
                chAutoLaunch.Checked = IsAutoLaunchStartup();
            }
        }

        private void SaveBatteryLevelSettings()
        {
            string serializedAlerts = BatteryDevice.All.Aggregate("",
                (acc, next) => acc + next.Id + ":::" + next.Threshold + "|||");
            Properties.Settings.Default.Alerts = serializedAlerts;
            Properties.Settings.Default.Save();
            SetAutoLaunchStartup();
            timer_Tick(this, new EventArgs());
        }

        private void SetAutoLaunchStartup()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rk == null) return;
            if (chAutoLaunch.Checked)
            {
                ProcessModule? module = Process.GetCurrentProcess().MainModule;
                if (module != null)
                    rk.SetValue(Application.ProductName, module.FileName);
            }
            else if (Application.ProductName != null)
                rk.DeleteValue(Application.ProductName, false);
        }

        private bool IsAutoLaunchStartup()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rk == null) return false;
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

        private async void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            await LoadBatterySettingsAsync();
            this.Show();
        }

        private async void timer_Tick(object sender, EventArgs e)
        {
            bool hasAlert = false;
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            List<string> batteriesLevels = new();
            await BatteryDevice.RefreshBatteriesLevel();
            foreach (var device in BatteryDevice.All.FindAll(b => b.Connected))
            {
                if (device.Level >= 0)
                {
                    batteriesLevels.Add($"{device.ShortName}: {device.Level}%");
                    hasAlert |= CheckBatteryLevelAndNotify(device, powerStatus);
                }
            }

            notifyIcon.BalloonTipText = string.Join("\n", batteriesLevels);
            notifyIcon.Text = notifyIcon.BalloonTipText.Length > 60 ? notifyIcon.BalloonTipText[..60] + "..." : notifyIcon.BalloonTipText;
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

        private bool CheckBatteryLevelAndNotify(BatteryDevice device, PowerStatus? powerStatus = null)
        {
            if (device.Connected && device.Level >= 0 && device.Level <= device.Threshold)
            {
                string? message = null;
                notifyIcon.Icon = Resources.LowBatteryAlert_red;
                if (device.Type == BatteryDevice.DeviceType.SystemBattery)
                {
                    if (powerStatus != null && !powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging))
                    {
                        message = $"Battery is {device.Level}%";
                    }
                }
                else
                {
                    message = $"{device.Name} battery is {device.Level}%";
                }
                if (message != null)
                {
                    notifyIcon.ShowBalloonTip(30000, "Low Battery Alert", message, ToolTipIcon.Warning);
                    return true;
                }
            }
            return false;
        }

        private async void lstBatteries_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstBatteries.SelectedItem != null)
            {
                var batteryDevice = (BatteryDevice)lstBatteries.SelectedItem;
                this.Cursor = Cursors.WaitCursor;
                this.Refresh();
                await batteryDevice.GetBatteryLevel();
                if (batteryDevice.Level >= 0)
                {
                    lblCurrentLevel.Text = batteryDevice.Level + "%";
                    numAlerttThreshold.Enabled = true;
                }
                else
                {
                    lblCurrentLevel.Text = "N/A";
                    numAlerttThreshold.Enabled = false;
                }
                numAlerttThreshold.Value = batteryDevice.Threshold;
                this.Cursor = Cursors.Default;
            }
        }

        private void numAlerttThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (lstBatteries.SelectedItem != null)
                ((BatteryDevice)lstBatteries.SelectedItem).Threshold = (int)numAlerttThreshold.Value;
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