using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace nxgmci.Net
{
    internal static class NetUtils
    {
        internal static bool EndPointFromUri(Uri Uri, out IPEndPoint EndPoint)
        {
            IPAddress ip = null;
            EndPoint = null;

            if (Uri == null)
                return false;

            if (Uri.HostNameType == UriHostNameType.IPv4 || Uri.HostNameType == UriHostNameType.IPv6)
            {
                if (!IPAddress.TryParse(Uri.Host, out ip))
                    return false;
            }
            else if (Uri.HostNameType == UriHostNameType.Dns)
            {
                try
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(Uri.Host);
                    if (addresses.Length == 0)
                        return false;
                    ip = addresses[0];
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
                return false;

            if (Uri.Port == 0 || Uri.Port >= ushort.MaxValue)
                return false;

            EndPoint = new IPEndPoint(ip, Uri.Port);
            return true;
        }
    }
}
