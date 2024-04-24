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
        public string Name;
        public string ShortName;
        public string Id;
        public int Threshold = 15;
        public int Level = -1;
        public bool Connected { get; private set; }
        public readonly DeviceType Type = DeviceType.SystemBattery;
        private readonly Battery? battery = null;
        private readonly BluetoothDevice? btDevice = null;
        private readonly BluetoothLEDevice? btLEDevice = null;

        public BatteryDevice(string id, string name, DeviceType type, bool connected, int threshold)
        {
            Id = id;
            ShortName = Name = name;
            Type = type;
            Connected = connected;
            Threshold = threshold;
        }

        private BatteryDevice(DeviceInformation device, Battery battery, int? idx = null)
        {
            this.battery = battery;
            Connected = true;
            Id = battery.DeviceId;
            string name = device.Id.Split('{')[0].Replace("\\", "").Replace("?", "").Trim('#');
            if (idx != null)
            {
                Name = "Battery " + idx + " (" + name + ")";
                ShortName = "Battery " + idx;
            }
            else
            {
                Name = "Battery (" + name + ")";
                ShortName = "Battery";
            }
            Type = DeviceType.SystemBattery;
            Debug.WriteLine(Name);
        }

        private BatteryDevice(DeviceInformation device, BluetoothDevice btDevice)
        {
            this.btDevice = btDevice;
            Id = device.Id;
            ShortName = Name = device.Name;
            Type = DeviceType.Bluetooth;
            Connected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
            Debug.WriteLine(device.Properties.First().Value);
        }

        private BatteryDevice(DeviceInformation device, BluetoothLEDevice btLEDevice)
        {
            this.btLEDevice = btLEDevice;
            Id = device.Id;
            ShortName = Name = device.Name;
            Type = DeviceType.BluetoothLE;
            Connected = btLEDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
            Debug.WriteLine(device.Properties.First().Value);
        }

        public override string? ToString()
        {
            return Name;
        }

        public void Dispose()
        {
            switch (Type)
            {
                case DeviceType.Bluetooth:
                    btDevice?.Dispose();
                    break;
                case DeviceType.BluetoothLE:
                    btLEDevice?.Dispose();
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
                _all ??= new List<BatteryDevice>();
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
            All.ForEach(b => b.Connected = false);
            string batterySelector = Battery.GetDeviceSelector();
            var batteriesInfo = await DeviceInformation.FindAllAsync(batterySelector);
            int idx = 1;
            foreach (DeviceInformation device in batteriesInfo.OrderBy(d => d.Id))
            {
                var d = await Battery.FromIdAsync(device.Id);
                var existing = All.Find(b => b.Id == device.Id);
                if (existing != null)
                    existing.Connected = true;
                else
                    All.Add(new(device, d, batteriesInfo.Count() > 0 ? idx : 0));
                idx++;
            }

            string btSelector = BluetoothDevice.GetDeviceSelector();
            var btInfo = await DeviceInformation.FindAllAsync(btSelector);
            foreach (DeviceInformation device in btInfo)
            {
                BluetoothDevice d = await BluetoothDevice.FromIdAsync(device.Id);
                var existing = All.Find(b => b.Id == device.Id);
                if (existing != null)
                    existing.Connected = d.ConnectionStatus == BluetoothConnectionStatus.Connected;
                else
                    All.Add(new(device, d));
            }

            string btLESelector = BluetoothLEDevice.GetDeviceSelector();
            var btLEInfo = await DeviceInformation.FindAllAsync(btLESelector);
            foreach (DeviceInformation device in btLEInfo)
            {
                BluetoothLEDevice d = await BluetoothLEDevice.FromIdAsync(device.Id);
                var existing = All.Find(b => b.Id == device.Id);
                if (existing != null)
                    existing.Connected = d.ConnectionStatus == BluetoothConnectionStatus.Connected;
                else
                    All.Add(new(device, d));
            }
            SortForList();
        }

        public static async Task RefreshBatteriesLevel()
        {
            foreach (var device in All.FindAll(b => b.Connected))
            {
                device.Level = await device.GetBatteryLevel();
                Debug.WriteLine(device.Name + ": " + device.Level);
            }
            SortForAlert();
        }

        private static void SortForAlert()
        {
            All.Sort((a, b) =>
            {
                if (a.Level == -1) return 1;
                if (a.Level == b.Level)
                {
                    if (a.Type == b.Type && b.Type == DeviceType.SystemBattery)
                        return a.Name.CompareTo(b.Name);
                    if (a.Type == DeviceType.SystemBattery)
                        return -1;
                    if (b.Type == DeviceType.SystemBattery)
                        return 1;
                }
                return (a.Level.CompareTo(b.Level));
            });
        }
        private static void SortForList()
        {
            All.Sort((a, b) =>
            {
                if (a.Type == b.Type && b.Type == DeviceType.SystemBattery)
                    return a.Name.CompareTo(b.Name);
                if (a.Type == DeviceType.SystemBattery)
                    return -1;
                if (b.Type == DeviceType.SystemBattery)
                    return 1;
                return a.Name.CompareTo(b.Name);
            });
        }
        #endregion

        public async Task<int> GetBatteryLevel()
        {
            Level = Type switch
            {
                DeviceType.Bluetooth => await GetBTBatteryLevel(),
                DeviceType.BluetoothLE => await GetBTLEBatteryLevel(),
                DeviceType.SystemBattery => GetSystemBatteryLevel(),
                _ => -1,
            };
            return Level;
        }

        private int GetSystemBatteryLevel()
        {
            if (battery == null)
                return -1;
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
                Connected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
                if (Connected)
                {
                    RfcommDeviceServicesResult rfcommResult = await btDevice.GetRfcommServicesAsync();
                    if (rfcommResult.Services.Count == 0)
                    {
                        Debug.WriteLine("No services found for " + Name + ". ");
                        Level = level;
                        return level;
                    }
                    // check if the device has battery level service
                    RfcommDeviceService selectedService = rfcommResult.Services.First(s => BluetoothService.All.Any(bs => bs.HasBattery && bs.Uuid == s.ServiceId.Uuid.ToString()));

                    if (selectedService == null)
                    {
                        Debug.WriteLine("No battery service found for " + Name);
                        Level = level;
                        return level;
                    }
                    try
                    {
                        using StreamSocket socket = new();
                        await socket.ConnectAsync(selectedService.ConnectionHostName, selectedService.ConnectionServiceName, SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
                        Debug.WriteLine("Connected to service for " + Name + ": " + selectedService.ServiceId.Uuid);
                        CancellationTokenSource source = new();
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
                    catch (Exception e)
                    {
                        Debug.WriteLine("Could not connect to service for " + Name + ". " + e.Message);
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
                Connected = btLEDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
                if (Connected)
                {
                    ////get UUID of Services
                    var services = await btLEDevice.GetGattServicesAsync();
                    if (services != null)
                    {
                        foreach (var service in services.Services)
                        {
                            //if there is a service thats same like the Battery Service
                            if (service.Uuid == GattServiceUuids.Battery)
                            {
                                var characteristics = await service.GetCharacteristicsAsync();
                                foreach (var characteristic in characteristics.Characteristics)
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
                                var battery = command[command.IndexOf(batCmd)..];
                                if (batCmd == "CBC")
                                    Level = Int32.Parse(battery[(battery.LastIndexOf(",") + 1)..]);
                                else
                                    Level = (Int32.Parse(battery[(battery.LastIndexOf(",") + 1)..]) + 1) * 10;
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
