using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nxgmci.Device;
using System.Net;

namespace nxgmci.Protocol.NVRAM
{
    public class NVRAMClient
    {
        public readonly EndpointDescriptor Endpoint;
        private readonly string requestBaseUrl;
        private readonly WebClient web;

        public NVRAMClient(EndpointDescriptor Endpoint)
        {
            // Input sanity checks
            // The actual port number is not yet validated since it could still change
            if (Endpoint == null)
                throw new ArgumentNullException("Endpoint");
            if (Endpoint.IPAddress == null)
                throw new NullReferenceException("Endpoint.IPAddress may not be null!");
            if(Endpoint.IPAddress.Length != 4 && Endpoint.IPAddress.Length != 16)
                throw new ArgumentOutOfRangeException("The IP address may only be 4 or 16 bytes (32 or 128 bits) long!");

            // Store the descriptor locally
            this.Endpoint = Endpoint;

            // Create the string builder
            StringBuilder urlBuilder = new StringBuilder("http://");

            // Assemble the IP
            for (int ipIndex = 0; ipIndex < Endpoint.IPAddress.Length; ipIndex++)
            {
                // If this is not the first byte, append a dot
                if (ipIndex != 0)
                    urlBuilder.Append('.');

                // Append the address byte
                urlBuilder.Append(Endpoint.IPAddress[ipIndex]);
            }

            // Assemble the rest of the request url
            urlBuilder.Append(':');
            urlBuilder.Append(Endpoint.PortNVRAMD);
            urlBuilder.Append('/');
            
            // Store the new url string
            requestBaseUrl = string.Copy(urlBuilder.ToString());
        }

        public Result Delete(string Field)
        {
            // Allocate the result object
            Result result = new Result();

            // Sanity check the input
            if (string.IsNullOrWhiteSpace(Field))
                return Result.FailMessage(result, "The field name is invalid!");

            // Try to make the request
            try
            {
                if (web.DownloadString(string.Format("{0}{1}?", requestBaseUrl, Field)).Trim().ToUpper() == "OK")
                    return Result.Succeed(result);
            }
            catch (Exception ex)
            {
                // Return failure due to an error
                return Result.FailErrorMessage(result, ex, "The deletion failed due to an error!");
            }

            // Return standard failure (probably an invalid item)
            return Result.FailMessage(result, "The deletion failed!");
        }

        public Result Set(string Field, string Value)
        {
            // Allocate the result object
            Result result = new Result();

            // Sanity check the input
            if (string.IsNullOrWhiteSpace(Field))
                return Result.FailMessage(result, "The field name is invalid!");

            // Check, if the value is null
            if (Value == null)
                Value = string.Empty;

            // Try to make the request
            try
            {
                if (web.DownloadString(string.Format("{0}{1}?{2}", requestBaseUrl, Field, Value)).Trim().ToUpper() == "OK")
                    return Result.Succeed(result);
            }
            catch (Exception ex)
            {
                // Return failure due to an error
                return Result.FailErrorMessage(result, ex, "The storage failed due to an error!");
            }

            // Return standard failure (probably an invalid item)
            return Result.FailMessage(result, "The storage failed!");
        }

        public Result<Dictionary<string, string>> GetAll()
        {
            return null;
        }

        public Result<string> Get(string Field)
        {
            return null;
        }
    }
}
