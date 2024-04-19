using System;
using System.Collections.Generic;
using System.Text;

namespace LowBatteryAlert
{
    internal class BatteryDevice
    {
        public enum DeviceType
        {
            SystemBattery,
            Bluetooth,
            BluetoothLE,
        }
        public string DeviceName = null;
        public string DeviceId = null;
        public double Level = 15;
        public DeviceType Type = DeviceType.SystemBattery;

        public override string ToString()
        {
            return DeviceName;
        }
    }
}
