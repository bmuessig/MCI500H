using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nxgmci.Device;
using System.Net;
using System.Threading;

namespace nxgmci.Protocol.NVRAM
{
    public class NVRAMClient
    {
        /// <summary>
        /// Endpoint instance that this NVRAMClient associates with.
        /// </summary>
        public readonly EndpointDescriptor Endpoint;

        // Private variables
        private readonly string requestBaseUrl;
        private readonly WebClient web;


        // TODO!!! Redo all this with regex, as parsing the response is not that simple as it's formatted HTML
        // Also make Delete call Set with null value argument

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="Endpoint">Endpoint instance that this NVRAMClient associates with.</param>
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

            // Create new webclient
            web = new WebClient();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Field"></param>
        /// <returns>A result object that indicates success or failure.</returns>
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

        /// <summary>
        /// Sets a single property on the device.
        /// </summary>
        /// <param name="Field">Name of the property to be set.</param>
        /// <param name="Value">Value of the property to be set. Null or an empty string deletes the property.</param>
        /// <returns>A result object that indicates success or failure.</returns>
        public Result Set(string Field, string Value, uint Retries = 0, uint DelayMillisecondsAfterTry = 100)
        {
            // Allocate the result object
            Result result = new Result();

            // Sanity check the input
            if (string.IsNullOrWhiteSpace(Field))
                return Result.FailMessage(result, "The field name is invalid!");

            // Check, if the value is null
            if (Value == null)
                Value = string.Empty;

            // Retry a given amount of times
            for (uint retry = 0; retry < Retries + 1; retry++)
            {
                // Check if the program should wait before the next attempt
                if (retry != 0)
                    Thread.Sleep((int)DelayMillisecondsAfterTry);

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
            }

            // Return standard failure (probably an invalid item)
            return Result.FailMessage(result, "The storage failed!");
        }

        /// <summary>
        /// Applies a number of properties to the device.
        /// </summary>
        /// <param name="Items"></param>
        /// <param name="Retries"></param>
        /// <param name="DelayMillisecondsAfterTry"></param>
        /// <returns>A result object that indicates success or failure.</returns>
        public Result SetMultiple(Dictionary<string, string> Items, uint Retries = 0, uint DelayMillisecondsAfterTry = 100)
        {
            // Allocate the result object and the item counter
            Result result = new Result();
            uint itemNo = 0;

            // Sanity check the input
            if (Items == null)
                return Result.FailMessage(result, "The item collection is null!");

            // Iterate through all items and set them
            foreach (KeyValuePair<string, string> item in Items)
            {
                // Skip invalid entries
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    itemNo++;
                    continue;
                }

                // Allocate the set result object
                Result setResult = null;

                // Retry a given amount of times
                for (uint retry = 0; retry < Retries + 1; retry++)
                {
                    // Check if the program should wait before the next attempt
                    if (retry != 0)
                        Thread.Sleep((int)DelayMillisecondsAfterTry);

                    // Set the value and check the result
                    if ((setResult = Set(item.Key, item.Value)).Success)
                        break;
                }

                // Check, if the result object is null (that should never happen)
                if (setResult != null)
                {
                    // Check, if the process succeeded
                    if (setResult.Success)
                    {
                        itemNo++;
                        continue;
                    }

                    // If there was an error, try to determine what failed
                    if (setResult.Error != null)
                        return Result.FailErrorMessage(result, setResult.Error, "Setting item #{0} failed {1} time(s). The last attempt failed due to an error!",
                            itemNo, Retries);
                }

                // It's not clear what failed
                return Result.FailMessage(result, "Setting item #{0} failed {1} time(s). The last attempt failed for unknown reasons!", itemNo, Retries);
            }

            // Only if setting all items completed successfully will this point be reached
            // So, return success
            return Result.Succeed(result);
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
