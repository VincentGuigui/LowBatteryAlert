using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Bluetooth;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Power;
using System.Runtime.CompilerServices;
using Windows.Security.Cryptography;

namespace LowBatteryAlert
{
    internal class BatteryDevice : IDisposable
    {
        internal enum DeviceType
        {
            SystemBattery,
            Bluetooth,
            BluetoothLE,
        }
        public string Name = null;
        public string Id = null;
        public int Threshold = 15;
        public int Level = -1;
        public bool Connected = true;
        public DeviceType Type = DeviceType.SystemBattery;
        private DeviceInformation Device = null;
        private Battery battery = null;
        private BluetoothDevice btDevice = null;
        private BluetoothLEDevice btLEDevice = null;

        public BatteryDevice()
        {
        }

        private BatteryDevice(DeviceInformation device, Battery battery)
        {
            Device = device;
            this.battery = battery;
            Id = battery.DeviceId;
            Name = "Battery (" + device.Id + ")";
            Type = DeviceType.SystemBattery;
            Debug.WriteLine(Name);
        }

        private BatteryDevice(DeviceInformation device, BluetoothDevice btDevice)
        {
            Device = device;
            this.btDevice = btDevice;
            Id = device.Id;
            Name = device.Name;
            Type = DeviceType.Bluetooth;
            Connected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
            Debug.WriteLine(device.Properties.First().Value);
        }

        private BatteryDevice(DeviceInformation device, BluetoothLEDevice btLEDevice)
        {
            Device = device;
            this.btLEDevice = btLEDevice;
            Id = device.Id;
            Name = device.Name;
            Type = DeviceType.BluetoothLE;
            Connected = btLEDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
            Debug.WriteLine(device.Properties.First().Value);
        }

        public override string ToString()
        {
            return Name;
        }

        public void Dispose()
        {
            switch (Type)
            {
                case DeviceType.Bluetooth:
                    btDevice.Dispose();
                    break;
                case DeviceType.BluetoothLE:
                    btLEDevice.Dispose();
                    break;
                default:
                    break;
            }
        }

        #region Static methods
        private static List<BatteryDevice>? _all = null;
        public static List<BatteryDevice> All
        {
            get
            {
                if (_all == null)
                    _all = new List<BatteryDevice>();
                return _all;
            }
        }

        public static BatteryDevice? Get(string id)
        {
            if (All.Count == 0)
                return null;
            return All.Find(b => b.Id == id);
        }

        public static async Task RefreshBatteriesList()
        {
            All.Clear();
            All.ForEach(b => b.Connected = false);
            string batterySelector = Battery.GetDeviceSelector();
            var batteriesInfo = await DeviceInformation.FindAllAsync(batterySelector);
            if (batteriesInfo.Count() > 0)
            {
                foreach (DeviceInformation device in batteriesInfo)
                {
                    var d = await Battery.FromIdAsync(device.Id);
                    var existing = All.Find(b => b.Id == device.Id);
                    if (existing != null)
                        existing.Connected = true;
                    else
                        All.Add(new(device, d));
                }
            }

            string btSelector = BluetoothDevice.GetDeviceSelector();
            var btInfo = await DeviceInformation.FindAllAsync(btSelector);
            if (btInfo.Count() > 0)
            {
                foreach (DeviceInformation device in btInfo)
                {
                    BluetoothDevice d = await BluetoothDevice.FromIdAsync(device.Id);
                    var existing = All.Find(b => b.Id == device.Id);
                    if (existing != null)
                        existing.Connected = true;
                    else
                        All.Add(new(device, d));
                }
            }

            string btLESelector = BluetoothLEDevice.GetDeviceSelector();
            var btLEInfo = await DeviceInformation.FindAllAsync(btLESelector);
            if (btLEInfo.Count() > 0)
            {
                foreach (DeviceInformation device in btLEInfo)
                {
                    BluetoothLEDevice d = await BluetoothLEDevice.FromIdAsync(device.Id);
                    var existing = All.Find(b => b.Id == device.Id);
                    if (existing != null)
                        existing.Connected = true;
                    else
                        All.Add(new(device, d));
                }
            }
            BatteryDevice.ReorderBatteriesPerLevelAndSystemFirst();
        }

        public static void ReorderBatteriesPerLevelAndSystemFirst()
        {
            All.Sort((a, b) =>
            {
                //if (a.Type == b.Type && b.Type == DeviceType.SystemBattery)
                //{
                //    if (a.Level == b.Level)
                //        return a.Name.CompareTo(b.Name);
                //    return a.Level.CompareTo(b.Level);
                //}
                //if (a.Type == DeviceType.SystemBattery)
                //    return -1;
                //if (b.Type == DeviceType.SystemBattery)
                //    return 1;
                return -(a.Level.CompareTo(b.Level));
            });
        }

        #endregion

        public async Task<int> GetBatteryLevel()
        {
            switch (Type)
            {
                case DeviceType.Bluetooth:
                    Level = await GetBTBatteryLevel();
                    break;
                case DeviceType.BluetoothLE:
                    Level = await GetBTLEBatteryLevel();
                    break;
                case DeviceType.SystemBattery:
                    Level = GetSystemBatteryLevel();
                    break;
                default:
                    Level = -1;
                    break;
            }
            return Level;
        }

        private int GetSystemBatteryLevel()
        {
            var report = battery.GetReport();
            var max = Convert.ToDouble(report.FullChargeCapacityInMilliwattHours);
            var level = Convert.ToDouble(report.RemainingCapacityInMilliwattHours);
            return (int)((level / max) * 100d);
        }

        private async Task<int> GetBTBatteryLevel()
        {
            int level = -1;
            if (btDevice != null)
            {
                if (btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    RfcommDeviceServicesResult rfcommResult = await btDevice.GetRfcommServicesAsync();
                    if (rfcommResult.Services.Count == 0)
                    {
                        Debug.WriteLine("No services found for " + Device.Name + ". ");
                        Level = level;
                        return level;
                    }
                    // check if the device has battery level service
                    RfcommDeviceService selectedService = rfcommResult.Services.First(s => BluetoothService.All.Any(bs => bs.HasBattery && bs.Uuid == s.ServiceId.Uuid.ToString()));

                    if (selectedService == null)
                    {
                        Debug.WriteLine("No battery service found for " + Device.Name);
                        Level = level;
                        return level;
                    }
                    try
                    {
                        using (StreamSocket socket = new StreamSocket())
                        {
                            await socket.ConnectAsync(selectedService.ConnectionHostName, selectedService.ConnectionServiceName, SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
                            Debug.WriteLine("Connected to service for " + Device.Name + ": " + selectedService.ServiceId.Uuid);
                            CancellationTokenSource source = new CancellationTokenSource();
                            CancellationToken cancelToken = source.Token;
                            Task listenOnChannel = await new TaskFactory().StartNew(async () =>
                            {
                                while (true)
                                {
                                    if (cancelToken.IsCancellationRequested)
                                    {
                                        break;
                                    }
                                    await Read(socket, source);
                                    break;
                                }
                            }, cancelToken);
                            listenOnChannel.Wait(3000);
                            selectedService.Dispose();
                            socket.Dispose();
                            return Level;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Could not connect to service for " + Device.Name + ". " + e.Message);
                    }
                }
            }
            Level = level;
            return level;
        }

        private async Task<int> GetBTLEBatteryLevel()
        {
            int level = -1;
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
                }
            }
            return level;
        }

        public async Task Read(StreamSocket socket, CancellationTokenSource source)
        {
            //https://radekp.github.io/qtmoko/api/modememulator-controlandstatus.html
            //https://developer.apple.com/accessories/Accessory-Design-Guidelines.pdf
            IBuffer buffer = new Windows.Storage.Streams.Buffer(1024);
            uint bytesRead = 1024;
            IBuffer result;
            while (bytesRead > 0)
            {
                try
                {
                    result = await socket.InputStream.ReadAsync(buffer, bytesRead, InputStreamOptions.Partial);
                    await Write(socket, "OK");
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Could not read from service " + e.Message);
                    source.Cancel();
                    return;
                }

                DataReader reader = DataReader.FromBuffer(result);
                var command = reader.ReadString(result.Length);
                if (command.Length != 0)
                {
                    string[] batCmds = { "CBC", "IPHONEACCEV" };
                    foreach (string batCmd in batCmds)
                    {
                        if (command.Contains(batCmd))
                        {
                            try
                            {
                                var battery = command.Substring(command.IndexOf(batCmd));
                                if (batCmd == "CBC")
                                    Level = Int32.Parse(battery.Substring(battery.LastIndexOf(",") + 1));
                                else
                                    Level = (Int32.Parse(battery.Substring(battery.LastIndexOf(",") + 1)) + 1) * 10;
                                source.Cancel();
                                return;
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine("Could not read battery from " + command + "." + e.Message);
                            }
                        }
                    }
                }
            }
            Debug.WriteLine("No BTAT commands received");
            source.Cancel();
        }

        public async Task Write(StreamSocket socket, string str)
        {
            var bytesWrite = CryptographicBuffer.ConvertStringToBinary("\r\n" + str + "\r\n", BinaryStringEncoding.Utf8);
            await socket.OutputStream.WriteAsync(bytesWrite);
        }
    }
}
