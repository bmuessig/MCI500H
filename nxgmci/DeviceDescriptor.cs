using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Drawing.Imaging;
using nxgmci.Device;

namespace nxgmci
{
    public class DeviceDescriptor
    {
        public DeviceDescriptor(string DeviceName)
        {

        }

        public readonly EndpointDescriptor Network;
        public readonly ScreenDescriptor Screen;
        public readonly DeviceModel Model;

        // These might be moved into a higher wrapper class
        // For instance they might be integrated into the MCI500H class
        public DeviceLanguage Language;
        public Version Version;
        public byte Volume;
        public uint UpdateID;

        /// <summary>
        /// Indicates the model of the device.
        /// </summary>
        public enum DeviceModel : byte
        {
            /// <summary>
            /// The model of the device is unknown.
            /// </summary>
            Unknown,

            /// <summary>
            /// Philips MCI500H.
            /// </summary>
            MCI500H
        }

        /// <summary>
        /// Indicates the device language.
        /// </summary>
        public enum DeviceLanguage : byte
        {

        }
    }
}
