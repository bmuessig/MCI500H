using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nxgmci.Device;

namespace nxgmci.Protocol.NVRAM
{
    public class NVRAMClient
    {
        public readonly EndpointDescriptor Endpoint;

        public NVRAMClient(EndpointDescriptor Endpoint)
        {
            // Input sanity checks
            // The actual port number is not yet validated since it could still change
            if (Endpoint == null)
                throw new ArgumentNullException("Endpoint");
            if (Endpoint.IPAddress == null)
                throw new NullReferenceException("Endpoint.IPAddress may not be null!");

            // Store the descriptor locally
            this.Endpoint = Endpoint;
        }

        public Result<Dictionary<string, string>> GetAll()
        {
            return null;
        }

        public Result<string> Get(string Field)
        {
            return null;
        }

        public struct QueryResult
        {
            bool Success;
            string ErrorMessage;

        }
    }
}
