using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.NVRAM
{
    public class NVRAM
    {
        public readonly DeviceDescriptor Device;

        public NVRAM(DeviceDescriptor Device)
        {
            // Input sanity checks
            // The actual port number is not yet validated since it could still change
            if (Device == null)
                throw new ArgumentNullException("Device");
            if (Device.IPAddress == null)
                throw new NullReferenceException("Device.IPAddress may not be null!");

            // Store the descriptor locally
            this.Device = Device;
        }

        public ActionResult<Dictionary<string, string>> GetAll()
        {
            return null;
        }

        public ActionResult<string> Get(string Field)
        {
            return new ActionResult<string>("");
        }

        public struct QueryResult
        {
            bool Success;
            string ErrorMessage;

        }
    }
}
