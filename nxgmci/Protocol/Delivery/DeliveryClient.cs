using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.Delivery
{
    public class DeliveryClient
    {
        public readonly DeviceDescriptor Descriptor;

        public DeliveryClient(DeviceDescriptor Descriptor)
        {
            this.Descriptor = Descriptor;
        }
    }
}
