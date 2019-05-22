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
                lock (updateIDLock)
                    return updateID;
            }

            set
            {
                lock (updateIDLock)
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
                lock (settingsLock)
                    return freezeUpdateID;
            }

            set
            {
                lock (settingsLock)
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
                lock (settingsLock)
                    return validateInput;
            }

            set
            {
                lock (settingsLock)
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
                lock (settingsLock)
                    return looseSyntax;
            }

            set
            {
                lock (settingsLock)
                    looseSyntax = value;
            }
        }

        /// <summary>
        /// This event is triggered during each API call, after the response from the device was received (or the connection timed out).
        /// </summary>
        public event EventHandler<ResultEventArgs<Postmaster.QueryResponse>> ResponseReceived;

        /// <summary>
        /// This event is triggered whenever the update ID changes.
        /// </summary>
        public event EventHandler<UpdateIDEventArgs> UpdateIDChanged;

        /// <summary>
        /// The internal update ID lock object used to prevent parallel execution and ensure thread safety.
        /// </summary>
        private readonly object updateIDLock = new object();

        /// <summary>
        /// The internal settings lock object used to prevent parallel execution and ensure thread safety.
        /// </summary>
        private readonly object settingsLock = new object();

        /// <summary>
        /// The internal IP endpoint of the WADM API.
        /// </summary>
        private readonly IPEndPoint ipEndpoint;

        /// <summary>
        /// The internal update ID.
        /// </summary>
        private volatile uint updateID = 0;

        /// <summary>
        /// An internal flag that indicates whether the update ID is frozen and will not be updated automatically.
        /// </summary>
        private volatile bool freezeUpdateID = false;

        /// <summary>
        /// An internal flag that indicates whether to validate the data values received.
        /// </summary>
        private volatile bool validateInput = true;

        /// <summary>
        /// An internal flag that indicates whether to ignore minor syntax errors.
        /// </summary>
        private volatile bool looseSyntax = false;

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

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

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
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "GetUpdateID")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.GetUpdateID.Parse(shadowResponse, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<GetUpdateID.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<GetUpdateID.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<GetUpdateID.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<GetUpdateID.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<GetUpdateID.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
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

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

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
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "QueryDatabase")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.QueryDatabase.Parse(shadowResponse, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<QueryDatabase.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<QueryDatabase.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<QueryDatabase.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<QueryDatabase.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<QueryDatabase.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
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

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
            }

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
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "QueryDiskSpace")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.QueryDiskSpace.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<QueryDiskSpace.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<QueryDiskSpace.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success and return it
            if (parseResult.Success)
                return Result<QueryDiskSpace.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<QueryDiskSpace.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<QueryDiskSpace.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request synchronizes all pending database changes to disk.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<SvcDbDump.ResponseParameters> SvcDbDump()
        {
            // Create the result object
            Result<SvcDbDump.ResponseParameters> result = new Result<SvcDbDump.ResponseParameters>();

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
            }

            // Allocate the response objects
            Postmaster.QueryResponse queryResponse;
            Result<SvcDbDump.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.SvcDbDump.Build(), true);

            // Check the result
            if (queryResponse == null)
                result.FailMessage("The query response was null!");
            else if (!queryResponse.Success)
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "SvcDbDump")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.SvcDbDump.Parse(shadowResponse, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<SvcDbDump.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<SvcDbDump.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success and return it
            if (parseResult.Success)
                return Result<SvcDbDump.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<SvcDbDump.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<SvcDbDump.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request returns a dictionary of all album IDs and their cleartext names.
        /// This information can be used to map the album IDs returned by RequestRawData to strings.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestIndexTable.ContentDataSet> RequestAlbumIndexTable()
        {
            // Create the result object
            Result<RequestIndexTable.ContentDataSet> result = new Result<RequestIndexTable.ContentDataSet>();

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

            // Allocate the response objects
            Postmaster.QueryResponse queryResponse;
            Result<RequestIndexTable.ContentDataSet> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestIndexTable.BuildAlbum(), true);

            // Check the result
            if (queryResponse == null)
                result.FailMessage("The query response was null!");
            else if (!queryResponse.Success)
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestAlbumIndexTable")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestIndexTable.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestIndexTable.ContentDataSet>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestIndexTable.ContentDataSet>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request returns a dictionary of all artist IDs and their cleartext names.
        /// This information can be used to map the artist IDs returned by RequestRawData to strings.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestIndexTable.ContentDataSet> RequestArtistIndexTable()
        {
            // Create the result object
            Result<RequestIndexTable.ContentDataSet> result = new Result<RequestIndexTable.ContentDataSet>();

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

            // Allocate the response objects
            Postmaster.QueryResponse queryResponse;
            Result<RequestIndexTable.ContentDataSet> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestIndexTable.BuildArtist(), true);

            // Check the result
            if (queryResponse == null)
                result.FailMessage("The query response was null!");
            else if (!queryResponse.Success)
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestArtistIndexTable")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestIndexTable.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestIndexTable.ContentDataSet>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestIndexTable.ContentDataSet>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request returns a dictionary of all genre IDs and their cleartext names.
        /// This information can be used to map the genre IDs returned by RequestRawData to strings.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestIndexTable.ContentDataSet> RequestGenreIndexTable()
        {
            // Create the result object
            Result<RequestIndexTable.ContentDataSet> result = new Result<RequestIndexTable.ContentDataSet>();

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

            // Allocate the response objects
            Postmaster.QueryResponse queryResponse;
            Result<RequestIndexTable.ContentDataSet> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestIndexTable.BuildGenre(), true);

            // Check the result
            if (queryResponse == null)
                result.FailMessage("The query response was null!");
            else if (!queryResponse.Success)
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestGenreIndexTable")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestIndexTable.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestIndexTable.ContentDataSet>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestIndexTable.ContentDataSet>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestIndexTable.ContentDataSet>.FailMessage(result, "The parsing failed due to an unknown reason!");
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

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

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
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestUriMetaData")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestUriMetaData.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestUriMetaData.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestUriMetaData.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestUriMetaData.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestUriMetaData.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestUriMetaData.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Attempts to create a new playlist with the given name.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Name">The name of the new playlist.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestPlaylistCreate.ResponseParameters> RequestPlaylistCreate(string Name)
        {
            // Create the result object
            Result<RequestPlaylistCreate.ResponseParameters> result = new Result<RequestPlaylistCreate.ResponseParameters>();

            // Perform input sanity checks
            if (Name == null)
                return Result<RequestPlaylistCreate.ResponseParameters>.FailMessage(result, "The argument '{0}' was null!", "Name");

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

            // Allocate the response objects
            Postmaster.QueryResponse queryResponse;
            Result<RequestPlaylistCreate.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestPlaylistCreate.Build(this.UpdateID, Name), true);

            // Check the result
            if (queryResponse == null)
                result.FailMessage("The query response was null!");
            else if (!queryResponse.Success)
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestPlaylistCreate")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestPlaylistCreate.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestPlaylistCreate.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestPlaylistCreate.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestPlaylistCreate.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestPlaylistCreate.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestPlaylistCreate.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Attempts to rename a playlist. The original name appears to be optional.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Index">The index of the playlist.</param>
        /// <param name="Name">The new name of the playlist.</param>
        /// <param name="OriginalName">The original name of the playlist.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestPlaylistRename.ResponseParameters> RequestPlaylistRename(uint Index, string Name, string OriginalName = null)
        {
            // Create the result object
            Result<RequestPlaylistRename.ResponseParameters> result = new Result<RequestPlaylistRename.ResponseParameters>();

            // Perform input sanity checks
            if (Name == null)
                return Result<RequestPlaylistRename.ResponseParameters>.FailMessage(result, "The argument '{0}' was null!", "Name");
            if (OriginalName == null)
                OriginalName = string.Empty;

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

            // Allocate the response objects
            Postmaster.QueryResponse queryResponse;
            Result<RequestPlaylistRename.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestPlaylistRename.Build(this.UpdateID, Index, Name, OriginalName), true);

            // Check the result
            if (queryResponse == null)
                result.FailMessage("The query response was null!");
            else if (!queryResponse.Success)
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestPlaylistRename")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestPlaylistRename.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestPlaylistRename.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestPlaylistRename.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestPlaylistRename.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestPlaylistRename.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestPlaylistRename.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Attempts to delete a playlist. The original name appears to be optional.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Index">The index of the playlist to be deleted.</param>
        /// <param name="OriginalName">The original name of the playlist to be deleted.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestPlaylistDelete.ResponseParameters> RequestPlaylistDelete(uint Index, string OriginalName = null)
        {
            // Create the result object
            Result<RequestPlaylistDelete.ResponseParameters> result = new Result<RequestPlaylistDelete.ResponseParameters>();

            // Perform an input sanity check
            if (OriginalName == null)
                OriginalName = string.Empty;

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

            // Allocate the response objects
            Postmaster.QueryResponse queryResponse;
            Result<RequestPlaylistDelete.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestPlaylistDelete.Build(this.UpdateID, Index, OriginalName), true);

            // Check the result
            if (queryResponse == null)
                result.FailMessage("The query response was null!");
            else if (!queryResponse.Success)
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestPlaylistDelete")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestPlaylistDelete.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestPlaylistDelete.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestPlaylistDelete.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestPlaylistDelete.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestPlaylistDelete.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestPlaylistDelete.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
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

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

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
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestRawData")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestRawData.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestRawData.ContentDataSet>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestRawData.ContentDataSet>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestRawData.ContentDataSet>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestRawData.ContentDataSet>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestRawData.ContentDataSet>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Internal function for fetching and browsing all database data using either RequestPlayableData or RequestNavData.
        /// </summary>
        /// <param name="RequestPlayable">If true, a RequestPlayableData is used to make the request. Otherwise RequestNavData is used.</param>
        /// <param name="NodeID">Parent node ID to fetch the child elements from.</param>
        /// <param name="NumElem">Maximum number of elements (0 returns all elements).</param>
        /// <param name="FromIndex">Offset index to base the query on.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        private Result<RequestPlayableNavData.ContentDataSet> RequestPlayableNavData(bool RequestPlayable, uint NodeID, uint NumElem, uint FromIndex)
        {
            // Create the result object
            Result<RequestPlayableNavData.ContentDataSet> result = new Result<RequestPlayableNavData.ContentDataSet>();

            // Allocate the temporary settings variables
            bool validateInput, looseSyntax, freezeUpdateID;

            // Fetch the settings thread-safe and ahead of time
            lock (settingsLock)
            {
                validateInput = this.validateInput;
                looseSyntax = this.looseSyntax;
                freezeUpdateID = this.freezeUpdateID;
            }

            // Allocate the response objects
            Postmaster.QueryResponse queryResponse;
            Result<RequestPlayableNavData.ContentDataSet> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            if (RequestPlayable)
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestPlayableNavData.BuildPlayable(NodeID, NumElem, FromIndex), true);
            else
                queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestPlayableNavData.BuildNav(NodeID, NumElem, FromIndex), true);

            // Check the result
            if (queryResponse == null)
                result.FailMessage("The query response was null!");
            else if (!queryResponse.Success)
                result.FailErrorMessage(new Exception(queryResponse.Message), "The query failed!");
            else if (!queryResponse.IsTextualReponse || string.IsNullOrWhiteSpace(queryResponse.TextualResponse))
                result.FailMessage("The query response was invalid!");
            else // Store a shadow copy of the response, as the query response is passed to the callee via an event and might later be compromised
                shadowResponse = string.Copy(queryResponse.TextualResponse.Trim());

            // Raise the event
            OnResponseReceived(new ResultEventArgs<Postmaster.QueryResponse>(
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, RequestPlayable ? "RequestPlayableData" : "RequestNavData")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestPlayableNavData.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestPlayableNavData.ContentDataSet>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestPlayableNavData.ContentDataSet>.FailMessage(result, "The parsed product was invalid!");

            // Check, if the result is a success
            if (parseResult.Success)
            {
                // Store the current and previous update ID, as well as allocate an flag that stores whether the field was updated
                uint newUpdateID = parseResult.Product.UpdateID, oldUpdateID = 0;
                bool wasUpdated = false;

                // Check, if the update ID may be updated automatically
                if (!freezeUpdateID && newUpdateID != 0)
                {
                    // Lock the updating for thread-safety
                    lock (updateIDLock)
                    {
                        // Store the previous update ID
                        oldUpdateID = this.updateID;

                        // If the two update IDs differ, update the old one
                        if ((wasUpdated = (oldUpdateID != newUpdateID)))
                            this.updateID = newUpdateID;
                    }
                }

                // Check, if anything was updated and raise the update event if true
                if (wasUpdated)
                    OnUpdateIDChanged(new UpdateIDEventArgs(newUpdateID, true, oldUpdateID));

                // Return the result
                return Result<RequestPlayableNavData.ContentDataSet>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestPlayableNavData.ContentDataSet>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestPlayableNavData.ContentDataSet>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// This request is used to fetch all playable database data in chunks and in a DLNA-like manner.
        /// It accepts a parent node ID and a max. items parameter (count).
        /// Using the parameters 0,0 will fetch the index.
        /// </summary>
        /// <param name="NodeID">Parent node ID of the query.</param>
        /// <param name="NumElem">Number of elements to be queried. Use zero to query all elements.</param>
        /// <param name="FromIndex">Offset index to base the query on.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestPlayableNavData.ContentDataSet> RequestPlayableData(uint NodeID = 0, uint NumElem = 0, uint FromIndex = 0)
        {
            return RequestPlayableNavData(true, NodeID, NumElem, FromIndex);
        }

        /// <summary>
        /// This request is used to fetch all navigation database data in chunks and in a DLNA-like manner.
        /// It accepts a parent node ID and a max. items parameter (count).
        /// Using the parameters 0,0 will fetch the index.
        /// </summary>
        /// <param name="NodeID">Parent node ID of the query.</param>
        /// <param name="NumElem">Number of elements to be queried. Use zero to query all elements.</param>
        /// <param name="FromIndex">Offset index to base the query on.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestPlayableNavData.ContentDataSet> RequestNavData(uint NodeID = 0, uint NumElem = 0, uint FromIndex = 0)
        {
            return RequestPlayableNavData(false, NodeID, NumElem, FromIndex);
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

        /// <summary>
        /// The internal event handler of the OnUpdateIDChanged event.
        /// </summary>
        /// <param name="e">The EventArgs.</param>
        protected virtual void OnUpdateIDChanged(UpdateIDEventArgs e)
        {
            // Get the event handler
            EventHandler<UpdateIDEventArgs> handler = UpdateIDChanged;

            // Sanity check and call it
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// EventArgs for update ID changes.
        /// </summary>
        public class UpdateIDEventArgs : EventArgs
        {
            /// <summary>
            /// Stores the new update ID.
            /// </summary>
            public readonly uint NewUpdateID;

            /// <summary>
            /// Stores the previous update ID.
            /// </summary>
            public readonly uint OldUpdateID;

            /// <summary>
            /// Indicates, whether the update ID had been updated automatically.
            /// </summary>
            public readonly bool AutomaticUpdate;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="NewUpdateID">The new update ID.</param>
            /// <param name="AutomaticUpdate">Indicates, whether the update ID had been updated automatically.</param>
            /// <param name="OldUpdateID">The previous update ID.</param>
            internal UpdateIDEventArgs(uint NewUpdateID, bool AutomaticUpdate, uint OldUpdateID = 0)
            {
                this.NewUpdateID = NewUpdateID;
                this.AutomaticUpdate = AutomaticUpdate;
                this.OldUpdateID = OldUpdateID;
            }
        }
    }
}
