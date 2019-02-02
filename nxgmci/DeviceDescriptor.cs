using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace nxgmci
{
    public class DeviceDescriptor
    {
        public const ushort DEFAULT_PORT_WADM = 8081, DEFAULT_PORT_NVRAMD = 6481,
            DEFAULT_PORT_DLNA_CLIENT = 8100, DEFAULT_PORT_DLNA_SERVER = 8084; // Client can also be 8080

        public IPAddress IPAddress;

        public ushort PortWADM;
        public ushort PortNVRAMD;
        public ushort PortDLNAClient;
        public ushort PortDLNAServer;
    }
}
