using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nxgmci.Network;
using nxgmci.Device;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This class provides a client for communicating with the WADM API.
    /// </summary>
    public class WADMClient
    {
        /// <summary>
        /// The default path of the WADM API endpoint relative to the server root.
        /// </summary>
        private const string DEFAULT_PATH = "/";

        /// <summary>
        /// The endpoint this instance of WADM associates with.
        /// </summary>
        public readonly EndpointDescriptor Endpoint;

        /// <summary>
        /// The path of the WADM API endpoint relative to the server root.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// The internal lock object used to prevent parallel execution and ensure thread safety.
        /// </summary>
        private volatile object eventLock = new object();

        /// <summary>
        /// The internal Uri to the WADM API endpoint.
        /// </summary>
        private readonly Uri endpointUri;

        /// <summary>
        /// This event is triggered during each API call, after the response from the device was received (or the connection timed out).
        /// </summary>
        public event EventHandler<ResultEventArgs<Postmaster.QueryResponse>> ResponseReceived;

        /// <summary>
        /// Default public constructor.
        /// </summary>
        /// <param name="Endpoint">The endpoint to associate with.</param>
        /// <param name="Path">The path of the WADM API endpoint relative to the server root.</param>
        public WADMClient(EndpointDescriptor Endpoint, string Path = DEFAULT_PATH)
        {
            // Store the endpoint
            this.Endpoint = Endpoint;

            // Process the path
            if (string.IsNullOrWhiteSpace(Path))
                Path = DEFAULT_PATH;

            // Make sure the path is absolute to the root
            if (!Path.StartsWith("/"))
                Path = string.Format("/{0}", Path);

            // Store the path
            this.Path = Path;

            // And build the internal endpoint Uri
            // This may fail, but that's the problem of the class creating this object
            endpointUri = new Uri(string.Format("http://{0}:{1}{2}", Endpoint.IPAddress, Endpoint.PortWADM, Path));
        }

        public Result<GetUpdateID.ResponseParameters> GetUpdateID(bool LazySyntax = false)
        {
            // Create the result object
            Result<GetUpdateID.ResponseParameters> result = new Result<GetUpdateID.ResponseParameters>();

            // Lock the class to ensure thread safety
            lock (eventLock)
            {
                // Allocate the response objects
                Postmaster.QueryResponse queryResponse;
                Result<GetUpdateID.ResponseParameters> parseResult;

                // Create the event result object
                Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

                // Allocate the shadow response text
                string shadowResponse = string.Empty;

                // Execute the request
                queryResponse = Postmaster.PostXML(endpointUri, WADM.GetUpdateID.Build(), true);

                // Check the result
                if (queryResponse == null)
                    result.FailMessage("The query response was null!");
                else if (!queryResponse.Success)
                    result.Fail("The query failed!", new Exception(queryResponse.Message));
                else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                    result.FailMessage("The query response was invalid!");
                else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromized
                    shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

                // Raise the event
                OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                    queryResult.Succeed(queryResponse, "GetUpdateID")));

                // Check, if the process failed
                if (result.Finalized)
                    return result;

                // Parse the response
                parseResult = WADM.GetUpdateID.Parse(shadowResponse, LazySyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success and return it
                if (parseResult.Success)
                    return result.Succeed(parseResult.Product, parseResult.Message);

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        public Result<QueryDatabase.ResponseParameters> QueryDatabase(bool LazySyntax = false)
        {
            // Create the result object
            Result<QueryDatabase.ResponseParameters> result = new Result<QueryDatabase.ResponseParameters>();

            // Lock the class to ensure thread safety
            lock (eventLock)
            {
                // Allocate the response objects
                Postmaster.QueryResponse queryResponse;
                Result<QueryDatabase.ResponseParameters> parseResult;

                // Create the event result object
                Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

                // Allocate the shadow response text
                string shadowResponse = string.Empty;

                // Execute the request
                queryResponse = Postmaster.PostXML(endpointUri, WADM.QueryDatabase.Build(), true);

                // Check the result
                if (queryResponse == null)
                    result.FailMessage("The query response was null!");
                else if (!queryResponse.Success)
                    result.Fail("The query failed!", new Exception(queryResponse.Message));
                else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                    result.FailMessage("The query response was invalid!");
                else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromized
                    shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

                // Raise the event
                OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                    queryResult.Succeed(queryResponse, "QueryDatabase")));

                // Check, if the process failed
                if (result.Finalized)
                    return result;

                // Parse the response
                parseResult = WADM.QueryDatabase.Parse(shadowResponse, LazySyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success and return it
                if (parseResult.Success)
                    return result.Succeed(parseResult.Product, parseResult.Message);

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        public Result<QueryDiskSpace.ResponseParameters> QueryDiskSpace(bool LazySyntax = false)
        {
            // Create the result object
            Result<QueryDiskSpace.ResponseParameters> result = new Result<QueryDiskSpace.ResponseParameters>();

            // Lock the class to ensure thread safety
            lock (eventLock)
            {
                // Allocate the response objects
                Postmaster.QueryResponse queryResponse;
                Result<QueryDiskSpace.ResponseParameters> parseResult;

                // Create the event result object
                Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

                // Allocate the shadow response text
                string shadowResponse = string.Empty;

                // Execute the request
                queryResponse = Postmaster.PostXML(endpointUri, WADM.QueryDiskSpace.Build(), true);

                // Check the result
                if (queryResponse == null)
                    result.FailMessage("The query response was null!");
                else if (!queryResponse.Success)
                    result.Fail("The query failed!", new Exception(queryResponse.Message));
                else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                    result.FailMessage("The query response was invalid!");
                else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromized
                    shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

                // Raise the event
                OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                    queryResult.Succeed(queryResponse, "QueryDiskSpace")));

                // Check, if the process failed
                if (result.Finalized)
                    return result;

                // Parse the response
                parseResult = WADM.QueryDiskSpace.Parse(shadowResponse, LazySyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success and return it
                if (parseResult.Success)
                    return result.Succeed(parseResult.Product, parseResult.Message);

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        public Result<RequestAlbumIndexTable.ContentDataSet> RequestAlbumIndexTable(bool LazySyntax = false)
        {
            // Create the result object
            Result<RequestAlbumIndexTable.ContentDataSet> result = new Result<RequestAlbumIndexTable.ContentDataSet>();

            // Lock the class to ensure thread safety
            lock (eventLock)
            {
                // Allocate the response objects
                Postmaster.QueryResponse queryResponse;
                Result<RequestAlbumIndexTable.ContentDataSet> parseResult;

                // Create the event result object
                Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

                // Allocate the shadow response text
                string shadowResponse = string.Empty;

                // Execute the request
                queryResponse = Postmaster.PostXML(endpointUri, WADM.RequestAlbumIndexTable.Build(), true);

                // Check the result
                if (queryResponse == null)
                    result.FailMessage("The query response was null!");
                else if (!queryResponse.Success)
                    result.Fail("The query failed!", new Exception(queryResponse.Message));
                else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                    result.FailMessage("The query response was invalid!");
                else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromized
                    shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

                // Raise the event
                OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                    queryResult.Succeed(queryResponse, "RequestAlbumIndexTable")));

                // Check, if the process failed
                if (result.Finalized)
                    return result;

                // Parse the response
                parseResult = WADM.RequestAlbumIndexTable.Parse(shadowResponse, LazySyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success and return it
                if (parseResult.Success)
                    return result.Succeed(parseResult.Product, parseResult.Message);

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        public Result<RequestArtistIndexTable.ContentDataSet> RequestArtistIndexTable(bool LazySyntax = false)
        {
            // Create the result object
            Result<RequestArtistIndexTable.ContentDataSet> result = new Result<RequestArtistIndexTable.ContentDataSet>();

            // Lock the class to ensure thread safety
            lock (eventLock)
            {
                // Allocate the response objects
                Postmaster.QueryResponse queryResponse;
                Result<RequestArtistIndexTable.ContentDataSet> parseResult;

                // Create the event result object
                Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

                // Allocate the shadow response text
                string shadowResponse = string.Empty;

                // Execute the request
                queryResponse = Postmaster.PostXML(endpointUri, WADM.RequestArtistIndexTable.Build(), true);

                // Check the result
                if (queryResponse == null)
                    result.FailMessage("The query response was null!");
                else if (!queryResponse.Success)
                    result.Fail("The query failed!", new Exception(queryResponse.Message));
                else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                    result.FailMessage("The query response was invalid!");
                else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromized
                    shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

                // Raise the event
                OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                    queryResult.Succeed(queryResponse, "RequestArtistIndexTable")));

                // Check, if the process failed
                if (result.Finalized)
                    return result;

                // Parse the response
                parseResult = WADM.RequestArtistIndexTable.Parse(shadowResponse, LazySyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success and return it
                if (parseResult.Success)
                    return result.Succeed(parseResult.Product, parseResult.Message);

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        public Result<RequestGenreIndexTable.ContentDataSet> RequestGenreIndexTable(bool LazySyntax = false)
        {
            // Create the result object
            Result<RequestGenreIndexTable.ContentDataSet> result = new Result<RequestGenreIndexTable.ContentDataSet>();

            // Lock the class to ensure thread safety
            lock (eventLock)
            {
                // Allocate the response objects
                Postmaster.QueryResponse queryResponse;
                Result<RequestGenreIndexTable.ContentDataSet> parseResult;

                // Create the event result object
                Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

                // Allocate the shadow response text
                string shadowResponse = string.Empty;

                // Execute the request
                queryResponse = Postmaster.PostXML(endpointUri, WADM.RequestGenreIndexTable.Build(), true);

                // Check the result
                if (queryResponse == null)
                    result.FailMessage("The query response was null!");
                else if (!queryResponse.Success)
                    result.Fail("The query failed!", new Exception(queryResponse.Message));
                else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                    result.FailMessage("The query response was invalid!");
                else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromized
                    shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

                // Raise the event
                OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                    queryResult.Succeed(queryResponse, "RequestGenreIndexTable")));

                // Check, if the process failed
                if (result.Finalized)
                    return result;

                // Parse the response
                parseResult = WADM.RequestGenreIndexTable.Parse(shadowResponse, LazySyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success and return it
                if (parseResult.Success)
                    return result.Succeed(parseResult.Product, parseResult.Message);

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        public Result<RequestUriMetaData.ResponseParameters> RequestUriMetaData(bool LazySyntax = false)
        {
            // Create the result object
            Result<RequestUriMetaData.ResponseParameters> result = new Result<RequestUriMetaData.ResponseParameters>();

            // Lock the class to ensure thread safety
            lock (eventLock)
            {
                // Allocate the response objects
                Postmaster.QueryResponse queryResponse;
                Result<RequestUriMetaData.ResponseParameters> parseResult;

                // Create the event result object
                Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

                // Allocate the shadow response text
                string shadowResponse = string.Empty;

                // Execute the request
                queryResponse = Postmaster.PostXML(endpointUri, WADM.RequestUriMetaData.Build(), true);

                // Check the result
                if (queryResponse == null)
                    result.FailMessage("The query response was null!");
                else if (!queryResponse.Success)
                    result.Fail("The query failed!", new Exception(queryResponse.Message));
                else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                    result.FailMessage("The query response was invalid!");
                else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromized
                    shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

                // Raise the event
                OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                    queryResult.Succeed(queryResponse, "RequestUriMetaData")));

                // Check, if the process failed
                if (result.Finalized)
                    return result;

                // Parse the response
                parseResult = WADM.RequestUriMetaData.Parse(shadowResponse, LazySyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success and return it
                if (parseResult.Success)
                    return result.Succeed(parseResult.Product, parseResult.Message);

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        public Result<RequestRawData.ContentDataSet> RequestRawData(uint FromIndex, uint NumElem = 0, bool LazySyntax = false)
        {
            // Create the result object
            Result<RequestRawData.ContentDataSet> result = new Result<RequestRawData.ContentDataSet>();

            // Lock the class to ensure thread safety
            lock (eventLock)
            {
                // Allocate the response objects
                Postmaster.QueryResponse queryResponse;
                Result<RequestRawData.ContentDataSet> parseResult;

                // Create the event result object
                Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

                // Allocate the shadow response text
                string shadowResponse = string.Empty;

                // Execute the request
                queryResponse = Postmaster.PostXML(endpointUri, WADM.RequestRawData.Build(FromIndex, NumElem), true);

                // Check the result
                if (queryResponse == null)
                    result.FailMessage("The query response was null!");
                else if (!queryResponse.Success)
                    result.Fail("The query failed!", new Exception(queryResponse.Message));
                else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                    result.FailMessage("The query response was invalid!");
                else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromized
                    shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

                // Raise the event
                OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                    queryResult.Succeed(queryResponse, "RequestRawData")));

                // Check, if the process failed
                if (result.Finalized)
                    return result;

                // Parse the response
                parseResult = WADM.RequestRawData.Parse(shadowResponse, LazySyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success and return it
                if (parseResult.Success)
                    return result.Succeed(parseResult.Product, parseResult.Message);

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// The internal event handler of the ResponseReceived event.
        /// </summary>
        /// <param name="e">The EventArgs.</param>
        protected virtual void OnResponseReceived(ResultEventArgs<Postmaster.QueryResponse> e)
        {
            // Get the event handler
            EventHandler<ResultEventArgs<Postmaster.QueryResponse>> handler = ResponseReceived;

            // Sanity check and call it
            if (handler != null)
                handler(this, e);
        }
    }
}
