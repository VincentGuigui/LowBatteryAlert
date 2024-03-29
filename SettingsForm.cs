using LowBatteryAlert.Properties;
using Microsoft.Win32;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;

namespace LowBatteryAlert
{
    public partial class SettingsForm : Form
    {
        Dictionary<string, int> batterySettings = new Dictionary<string, int>();
        List<Battery> systemBatteries = new List<Battery>();

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

            // Find batteries 
            var deviceInfo = await DeviceInformation.FindAllAsync(Battery.GetDeviceSelector());
            if (deviceInfo.Count() == 0)
            {
                return;
            }
            else
            {

                foreach (DeviceInformation device in deviceInfo)
                {
                    var battery = await Battery.FromIdAsync(device.Id);
                    systemBatteries.Add(battery);
                }
            }
        }

        private double GetBatteryLevel(Battery battery)
        {
            return GetBatteryLevel(battery, out _);
        }

        private double GetBatteryLevel(Battery battery, out BatteryReport report)
        {
            report = battery.GetReport();
            var max = Convert.ToDouble(report.FullChargeCapacityInMilliwattHours);
            var level = Convert.ToDouble(report.RemainingCapacityInMilliwattHours);
            return (level / max) * 100d;
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
            await GetSytemBatteriesListAsync();
            batterySettings.Clear();
            if (systemBatteries.Count == 0)
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
                var deserializedAlerts = new Dictionary<string, int>();
                if (!string.IsNullOrEmpty(Properties.Settings.Default.Alerts))
                    foreach (var alert in Properties.Settings.Default.Alerts.Split("|||"))
                    {
                        if (!string.IsNullOrEmpty(alert))
                            deserializedAlerts.Add(alert.Split(":::")[0], Convert.ToInt32(alert.Split(":::")[1]));
                    }

                foreach (var battery in systemBatteries.OrderBy(b => b.DeviceId))
                {
                    lstBatteries.Items.Add(battery.DeviceId);
                    if (deserializedAlerts.ContainsKey(battery.DeviceId))
                    {
                        batterySettings.Add(battery.DeviceId, deserializedAlerts[battery.DeviceId]);
                    }
                    else
                    {
                        batterySettings.Add(battery.DeviceId, 15);
                    }
                }
                lstBatteries.SelectedIndex = 0;
                chAutoLaunch.Checked = IsAutoLaunchStartup();
            }
        }

        private void SaveBatteryLevelSettings()
        {
            string serializedAlerts = batterySettings.Aggregate("",
                (acc, next) => acc + next.Key + ":::" + next.Value + "|||");
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

        private void timer_Tick(object sender, EventArgs e)
        {
            bool hasAlert = false;
            PowerStatus powerStatus = SystemInformation.PowerStatus;
            string batteriesLevel = "";
            int i = 0;
            foreach (var battery in systemBatteries.OrderBy(b => b.DeviceId))
            {
                var level = GetBatteryLevel(battery);
                if (systemBatteries.Count > 1)
                    batteriesLevel += $"Battery {i + 1}: {level.ToString("F0")}%\n";
                else
                    batteriesLevel = $"{level.ToString("F0")}%\n";
                if (level <= batterySettings[battery.DeviceId])
                {
                    hasAlert = true;
                    notifyIcon.Icon = Resources.LowBatteryAlert_red;

                    if (!powerStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging))
                    {
                        notifyIcon.ShowBalloonTip(30000,
                            "Low Battery Alert", $"Battery level is {level.ToString("F0")}%.",
                            ToolTipIcon.Warning);
                    }
                }
                i++;
            }
            notifyIcon.BalloonTipText = notifyIcon.Text = Application.ProductName + ":\n" + batteriesLevel;
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

        private void lstBatteries_SelectedIndexChanged(object sender, EventArgs e)
        {
            var battery = systemBatteries.SingleOrDefault(b => b.DeviceId == lstBatteries.SelectedItem?.ToString());
            if (battery != null)
                lblCurrentLevel.Text = GetBatteryLevel(battery).ToString("F2") + "%";
            if (lstBatteries.SelectedItem != null)
                numAlertLevel.Value = batterySettings[lstBatteries.SelectedItem.ToString()];
        }

        private void numAlertLevel_ValueChanged(object sender, EventArgs e)
        {
            if (lstBatteries.SelectedItem != null)
                batterySettings[lstBatteries.SelectedItem.ToString()] = (int)numAlertLevel.Value;
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