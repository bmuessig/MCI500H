using System;
using System.Net;

namespace nxgmci.Device
{
    /// <summary>
    /// Provides the network connection details for all APIs.
    /// </summary>
    public class EndpointDescriptor
    {
        /// <summary>
        /// The default port of the WADM API.
        /// </summary>
        public const ushort DEFAULT_PORT_WADM = 8081;

        /// <summary>
        /// The default port of the nvramd API.
        /// </summary>
        public const ushort DEFAULT_PORT_NVRAMD = 6481;

        /// <summary>
        /// The default port of the DLNA client. There is an equivalent port available at 8080.
        /// </summary>
        public const ushort DEFAULT_PORT_DLNA_CLIENT = 8100;

        /// <summary>
        /// The default port of the DLNA server.
        /// </summary>
        public const ushort DEFAULT_PORT_DLNA_SERVER = 8084;

        /// <summary>
        /// The private copy of the IP address.
        /// </summary>
        private readonly IPAddress ipAddress;

        // TODO, preliminary
        /// <summary>
        /// Returns the string representation of the hostname or IP address of this instance.
        /// </summary>
        public string Host
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the bytes of the IP address.
        /// </summary>
        public byte[] IPAddress
        {
            get
            {
                return ipAddress.GetAddressBytes();
            }
        }

        /// <summary>
        /// The port used for the WADM API.
        /// </summary>
        public readonly ushort PortWADM;

        /// <summary>
        /// The port used for the nvramd API.
        /// </summary>
        public readonly ushort PortNVRAMD;

        /// <summary>
        /// The port used for the DLNA client.
        /// </summary>
        public readonly ushort PortDLNAClient;

        /// <summary>
        /// The port used for the DLNA server.
        /// </summary>
        public readonly ushort PortDLNAServer;

        /// <summary>
        /// The default public constructor.
        /// </summary>
        /// <param name="IPAddress">The IP address of the device.</param>
        /// <param name="PortWADM">The port used for the WADM API.</param>
        /// <param name="PortDLNAClient">The port used for the DLNA client.</param>
        /// <param name="PortNVRAMD">The port used for the nvramd API.</param>
        /// <param name="PortDLNAServer">The port used for the DLNA server.</param>
        public EndpointDescriptor(IPAddress IPAddress, ushort PortWADM = DEFAULT_PORT_WADM, ushort PortDLNAClient = DEFAULT_PORT_DLNA_CLIENT,
            ushort PortNVRAMD = DEFAULT_PORT_NVRAMD, ushort PortDLNAServer = DEFAULT_PORT_DLNA_SERVER)
        {
            // Sanity checks
            if (IPAddress == null)
                throw new ArgumentNullException("IPAddress");
            if (PortWADM == 0)
                throw new ArgumentOutOfRangeException("PortWADM");
            if (PortDLNAClient == 0)
                throw new ArgumentOutOfRangeException("PortDLNAClient");
            // The other ports are not checked intentionally, as they are left optional and may be equal to zero

            // Duplicate the IP address
            this.ipAddress = new IPAddress(IPAddress.GetAddressBytes());

            // Copy the other values
            this.PortWADM = PortWADM;
            this.PortDLNAClient = PortDLNAClient;
            this.PortNVRAMD = PortNVRAMD;
            this.PortDLNAServer = PortDLNAServer;
        }
    }
}