using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nxgmci.Network;
using nxgmci.Device;
using System.Net;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This class provides a thread-safe client for communicating with the WADM API.
    /// </summary>
    public class WADMClient
    {
        /// <summary>
        /// The endpoint this instance of WADM associates with.
        /// </summary>
        public readonly EndpointDescriptor Endpoint;

        /// <summary>
        /// The path of the WADM API endpoint relative to the server root.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Gets or sets the update ID included with the next request.
        /// This is thread-safe.
        /// </summary>
        public uint UpdateID
        {
            get
            {
                lock (eventLock)
                    return updateID;
            }

            set
            {
                lock (eventLock)
                    updateID = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the update ID is frozen and therefore not automatically updated.
        /// This is thread-safe.
        /// </summary>
        public bool FreezeUpdateID
        {
            get
            {
                lock (eventLock)
                    return freezeUpdateID;
            }

            set
            {
                lock (eventLock)
                    freezeUpdateID = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to validate the data values received.
        /// This is thread-safe.
        /// </summary>
        public bool ValidateInput
        {
            get
            {
                lock (eventLock)
                    return validateInput;
            }

            set
            {
                lock (eventLock)
                    validateInput = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to ignore minor syntax errors.
        /// This is thread-safe.
        /// </summary>
        public bool LooseSyntax
        {
            get
            {
                lock (eventLock)
                    return looseSyntax;
            }

            set
            {
                lock (eventLock)
                    looseSyntax = value;
            }
        }

        /// <summary>
        /// This event is triggered during each API call, after the response from the device was received (or the connection timed out).
        /// </summary>
        public event EventHandler<ResultEventArgs<Postmaster.QueryResponse>> ResponseReceived;

        /// <summary>
        /// The internal lock object used to prevent parallel execution and ensure thread safety.
        /// </summary>
        private volatile object eventLock = new object();

        /// <summary>
        /// The internal IP endpoint of the WADM API.
        /// </summary>
        private readonly IPEndPoint ipEndpoint;

        /// <summary>
        /// The internal update ID.
        /// </summary>
        private uint updateID = 0;

        /// <summary>
        /// An internal flag that indicates whether the update ID is frozen and will not be updated automatically.
        /// </summary>
        private bool freezeUpdateID = false;

        /// <summary>
        /// An internal flag that indicates whether to validate the data values received.
        /// </summary>
        private bool validateInput = true;

        /// <summary>
        /// An internal flag that indicates whether to ignore minor syntax errors.
        /// </summary>
        private bool looseSyntax = false;

        /// <summary>
        /// The default path of the WADM API endpoint relative to the server root.
        /// </summary>
        private const string DEFAULT_PATH = "/";

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

            // And build the internal endpoint
            // TODO: Sanity check the IP
            ipEndpoint = new IPEndPoint(new IPAddress(Endpoint.IPAddress), (int)Endpoint.PortWADM);
        }

        /// <summary>
        /// This request returns the current update ID. This update ID has to be included into all destructive requests.
        /// The update ID automatically increments by one after it has been used.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<GetUpdateID.ResponseParameters> GetUpdateID()
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
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.GetUpdateID.Build(), true);

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
                parseResult = WADM.GetUpdateID.Parse(shadowResponse, this.looseSyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success
                if (parseResult.Success)
                {
                    // Also, store the update ID
                    if (parseResult.Product.UpdateID != this.updateID && parseResult.Product.UpdateID != 0 && !freezeUpdateID)
                        this.updateID = parseResult.Product.UpdateID;

                    // Return the result
                    return result.Succeed(parseResult.Product, parseResult.Message);
                }

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request returns some library statistics along the current update ID.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<QueryDatabase.ResponseParameters> QueryDatabase()
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
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.QueryDatabase.Build(), true);

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
                parseResult = WADM.QueryDatabase.Parse(shadowResponse, this.looseSyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success
                if (parseResult.Success)
                {
                    // Also, store the update ID
                    if (parseResult.Product.UpdateID != this.updateID && parseResult.Product.UpdateID != 0 && !freezeUpdateID)
                        this.updateID = parseResult.Product.UpdateID;

                    // Return the result
                    return result.Succeed(parseResult.Product, parseResult.Message);
                }

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request returns both the free and used harddisk space.
        /// This information can be used to determine whether new files could be uploaded or not.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<QueryDiskSpace.ResponseParameters> QueryDiskSpace()
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
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.QueryDiskSpace.Build(), true);

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
                parseResult = WADM.QueryDiskSpace.Parse(shadowResponse, this.validateInput, this.looseSyntax);

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
        /// This request returns a dictionary of all album IDs and their cleartext names.
        /// This information can be used to map the album IDs returned by RequestRawData to strings.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestAlbumIndexTable.ContentDataSet> RequestAlbumIndexTable()
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
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestAlbumIndexTable.Build(), true);

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
                parseResult = WADM.RequestAlbumIndexTable.Parse(shadowResponse, this.validateInput, this.looseSyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success
                if (parseResult.Success)
                {
                    // Also, store the update ID
                    if (parseResult.Product.UpdateID != this.updateID && parseResult.Product.UpdateID != 0 && !freezeUpdateID)
                        this.updateID = parseResult.Product.UpdateID;

                    // Return the result
                    return result.Succeed(parseResult.Product, parseResult.Message);
                }

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request returns a dictionary of all artist IDs and their cleartext names.
        /// This information can be used to map the artist IDs returned by RequestRawData to strings.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestArtistIndexTable.ContentDataSet> RequestArtistIndexTable()
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
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestArtistIndexTable.Build(), true);

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
                parseResult = WADM.RequestArtistIndexTable.Parse(shadowResponse, this.validateInput, this.looseSyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success
                if (parseResult.Success)
                {
                    // Also, store the update ID
                    if (parseResult.Product.UpdateID != this.updateID && parseResult.Product.UpdateID != 0 && !freezeUpdateID)
                        this.updateID = parseResult.Product.UpdateID;

                    // Return the result
                    return result.Succeed(parseResult.Product, parseResult.Message);
                }

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request returns a dictionary of all genre IDs and their cleartext names.
        /// This information can be used to map the genre IDs returned by RequestRawData to strings.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestGenreIndexTable.ContentDataSet> RequestGenreIndexTable()
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
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestGenreIndexTable.Build(), true);

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
                parseResult = WADM.RequestGenreIndexTable.Parse(shadowResponse, this.validateInput, this.looseSyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success
                if (parseResult.Success)
                {
                    // Also, store the update ID
                    if (parseResult.Product.UpdateID != this.updateID && parseResult.Product.UpdateID != 0 && !freezeUpdateID)
                        this.updateID = parseResult.Product.UpdateID;

                    // Return the result
                    return result.Succeed(parseResult.Product, parseResult.Message);
                }

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request is used to retrieve lots of important flags from the stereo.
        /// For instance, this will provide the public directory of the media files.
        /// It will also provide the bitmask mask that needs to be applied to work with the node IDs.
        /// Apart from that it presents a list of supported file formats along with their type-ID mapping.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestUriMetaData.ResponseParameters> RequestUriMetaData()
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
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestUriMetaData.Build(), true);

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
                parseResult = WADM.RequestUriMetaData.Parse(shadowResponse, this.validateInput, this.looseSyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success
                if (parseResult.Success)
                {
                    // Also, store the update ID
                    if (parseResult.Product.UpdateID != this.updateID && parseResult.Product.UpdateID != 0 && !freezeUpdateID)
                        this.updateID = parseResult.Product.UpdateID;

                    // Return the result
                    return result.Succeed(parseResult.Product, parseResult.Message);
                }

                // Try to return a detailed error
                if (parseResult.Error != null)
                    return result.FailMessage("The parsing failed!", parseResult.Error);
            }

            // If not possible, return simple failure
            return result.FailMessage("The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request is used to fetch title data in chunks or as a whole.
        /// It accepts both a start index (skip) and a max. items parameter (count).
        /// Using the parameters 0,0 will fetch all titles. This is not recommended for large databases.
        /// The stereo has limited RAM and processing capabilities and a database with 1000s of titles may overflow.
        /// It is recommended to fetch 100 titles at a time. The first request will return a total number of titles.
        /// This number can be used to generate the correct number of requests to fetch all titles successfully.
        /// The 0,0 method is not used by the official application, whereas the 100 element method is used.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="FromIndex">First index to be included into the query.</param>
        /// <param name="NumElem">Number of elements to be queried. Use zero to query all elements.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestRawData.ContentDataSet> RequestRawData(uint FromIndex, uint NumElem = 0)
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
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestRawData.Build(FromIndex, NumElem), true);

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
                parseResult = WADM.RequestRawData.Parse(shadowResponse, this.validateInput, this.looseSyntax);

                // Sanity check the result
                if (parseResult == null)
                    return result.FailMessage("The parsed result was null!");
                if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                    return result.FailMessage("The parsed product was invalid!");

                // Check, if the result is a success
                if (parseResult.Success)
                {
                    // Also, store the update ID
                    if (parseResult.Product.UpdateID != this.updateID && parseResult.Product.UpdateID != 0 && !freezeUpdateID)
                        this.updateID = parseResult.Product.UpdateID;

                    // Return the result
                    return result.Succeed(parseResult.Product, parseResult.Message);
                }

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
