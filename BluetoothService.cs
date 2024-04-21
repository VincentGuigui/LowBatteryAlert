using System;
using System.Collections.Generic;
using System.Text;

namespace LowBatteryAlert
{
    public class BluetoothService
    {
        // https://learn.microsoft.com/fr-fr/windows/client-management/mdm/policy-csp-bluetooth#servicesallowedlist-usage-guide
        public static List<BluetoothService> All = new List<BluetoothService>() 
        {
            new () {Uuid = "00001105-0000-1000-8000-00805f9b34fb", Name = "OBEX Object Push"},
            new () {Uuid = "00001108-0000-1000-8000-00805f9b34fb", Name = "Headset (old)", HasBattery = true},
            new () {Uuid = "0000110a-0000-1000-8000-00805f9b34fb", Name = "Audio Source", HasBattery = true},
            new () {Uuid = "0000110b-0000-1000-8000-00805f9b34fb", Name = "Audio A2DP", HasBattery = true},
            new () {Uuid = "0000110c-0000-1000-8000-00805f9b34fb", Name = "A/V Remote Control Target"},
            new () {Uuid = "0000110e-0000-1000-8000-00805f9b34fb", Name = "A/V Remote Control"},
            new () {Uuid = "0000110f-0000-1000-8000-00805f9b34fb", Name = "A/V Remote Control Controller"},
            new () {Uuid = "00001112-0000-1000-8000-00805f9b34fb", Name = "Handsfree", HasBattery = true},
            new () {Uuid = "00001115-0000-1000-8000-00805f9b34fb", Name = "PANU"},
            new () {Uuid = "00001116-0000-1000-8000-00805f9b34fb", Name = "NAP"},
            new () {Uuid = "0000111e-0000-1000-8000-00805f9b34fb", Name = "Handsfree Audio Gateway", HasBattery = true},
            new () {Uuid = "0000111f-0000-1000-8000-00805f9b34fb", Name = "Handsfree Audio Gateway", HasBattery = true},
            new () {Uuid = "00001124-0000-1000-8000-00805f9b34fb", Name = "HID (Human Interface Device)", HasBattery = true},
            new () {Uuid = "0000112d-0000-1000-8000-00805f9b34fb", Name = "SIM Access"},
            new () {Uuid = "0000112f-0000-1000-8000-00805f9b34fb", Name = "Phonebook Access"},
            new () {Uuid = "00001132-0000-1000-8000-00805f9b34fb", Name = "Message Access"},
            new () {Uuid = "00001200-0000-1000-8000-00805f9b34fb", Name = "PnP Information"},
            new () {Uuid = "00001203-0000-1000-8000-00805f9b34fb", Name = "Generic Audio Service", HasBattery = true},
            new () {Uuid = "00001800-0000-1000-8000-00805f9b34fb", Name = "Generic Access  Profile"},
            new () {Uuid = "00001801-0000-1000-8000-00805f9b34fb", Name = "Generic Attribute Profile"},
            new () {Uuid = "00001812-0000-1000-8000-00805f9b34fb", Name = "HID sur GATT", HasBattery = true},
            new () {Uuid = "3a622e58-c035-4a26-928c-5601dde7c657", Name = "Vendor specific"},
            new () {Uuid = "a23d00bc-217c-123b-9c00-fc44577136ee", Name = "Vendor specific"},
            new () {Uuid = "a82efa21-ae5c-3dde-9bbc-f16da7b16c5a", Name = "Vendor specific"},
            new () {Uuid = "f8d1fbe4-7966-4334-8024-ff96c9330e15", Name = "Bose specific"},
            new () {Uuid = "81c2e72a-0591-443e-a1ff-05f988593351", Name = "Bose specific"},
            new () {Uuid = "931c7e8a-540f-4686-b798-e8df0a2ad9f7", Name = "Bose specific"},
        };

        public string Uuid { get; set; }
        public string Name { get; set; } = "Unknown";
        public bool HasBattery { get; set; }
    }
}
