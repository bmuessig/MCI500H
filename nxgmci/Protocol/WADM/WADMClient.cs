﻿using System;
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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.GetUpdateID.Build();

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.QueryDatabase.Build();

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.QueryDiskSpace.Build();

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.SvcDbDump.Build();

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestIndexTable.BuildAlbum();

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestIndexTable.BuildArtist();

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestIndexTable.BuildGenre();

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestUriMetaData.Build();

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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
        /// If the playlist name is null or white-space it will be replaced with 'New Playlist'.
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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestPlaylistCreate.Build(this.UpdateID, Name);

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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
        /// If the new playlist name is null or white-space it will be replaced with 'New Playlist'.
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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestPlaylistRename.Build(this.UpdateID, Index, Name, OriginalName);

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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
        /// Attempts to add a track to a playlist.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="TargetIndex">The index of the playlist to insert the track into.</param>
        /// <param name="SourceIndex">The index of the track to insert into the playlist.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestPlaylistTrackInsert.ResponseParameters> RequestPlaylistTrackAdd(uint TargetIndex, uint SourceIndex)
        {
            // Create the result object
            Result<RequestPlaylistTrackInsert.ResponseParameters> result = new Result<RequestPlaylistTrackInsert.ResponseParameters>();

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
            Result<RequestPlaylistTrackInsert.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestPlaylistTrackInsert.BuildAdd(this.UpdateID, TargetIndex, SourceIndex);

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestPlaylistTrackAdd")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestPlaylistTrackInsert.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestPlaylistTrackInsert.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestPlaylistTrackInsert.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

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
                return Result<RequestPlaylistTrackInsert.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestPlaylistTrackInsert.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestPlaylistTrackInsert.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Attempts to change the position of a track within a playlist.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="TargetIndex">The the parent playlist ID of the item to be moved.</param>
        /// <param name="SourceIndex">The ID of the item to be moved inside the parent's namespace.</param>
        /// <param name="Offset">Unknown offset. Might be offset from the top.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestPlaylistTrackInsert.ResponseParameters> RequestPlaylistTrackMove(uint TargetIndex, uint SourceIndex, uint Offset)
        {
            // Create the result object
            Result<RequestPlaylistTrackInsert.ResponseParameters> result = new Result<RequestPlaylistTrackInsert.ResponseParameters>();

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
            Result<RequestPlaylistTrackInsert.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestPlaylistTrackInsert.BuildMove(this.UpdateID, TargetIndex, SourceIndex, Offset);

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestPlaylistTrackMove")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestPlaylistTrackInsert.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestPlaylistTrackInsert.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestPlaylistTrackInsert.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

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
                return Result<RequestPlaylistTrackInsert.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestPlaylistTrackInsert.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestPlaylistTrackInsert.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Attempts to delete a track from a playlist or an entire playlist at once.
        /// Depending on what node ID is passed as the index parameter, the function will behave accordingly.
        /// Note, that when deleting a track from a playlist, the node ID with the correct namespace has to be passed.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Index">The index of the playlist or track to be deleted.</param>
        /// <param name="OriginalName">The original name of the playlist or track to be deleted.</param>
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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;
            
            // Build the query
            outboundQuery = WADM.RequestPlaylistDelete.Build(this.UpdateID, Index, OriginalName);
            
            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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
        /// Attempts to create a new media file with album art.
        /// After this initial creation step, the file (and optional encrypted cover art) has to be uploaded via DeliveryClient within the timeout period.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Artist">The artist of the track.</param>
        /// <param name="Album">The album of the track.</param>
        /// <param name="Genre">The genre of the track according to the genre list.</param>
        /// <param name="Name">The title of the track.</param>
        /// <param name="TrackNum">The number of the track.</param>
        /// <param name="Year">The year that the track was from.</param>
        /// <param name="MediaType">The three letter file extension of the media file.</param>
        /// <param name="DMMCookie">The unknown DMMCookie (seems to be ignored).</param>
        /// <param name="Timeout">The upload timeout in seconds.</param>
        /// <param name="AlbumArtHash">The MD5 hash of the primariy album art.</param>
        /// <param name="AlbumArtFileSize">The file size in bytes of the primary album art.</param>
        /// <param name="AlbumArtTnFileSize">The file size in bytes of the thumbnail of the album art.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestObjectCreate.ResponseParameters> RequestObjectCreate(
            string Artist, string Album, string Genre, string Name, uint TrackNum, uint Year,
            string MediaType, uint DMMCookie, uint Timeout, string AlbumArtHash, uint AlbumArtFileSize, uint AlbumArtTnFileSize)
        {
            // Create the result object
            Result<RequestObjectCreate.ResponseParameters> result = new Result<RequestObjectCreate.ResponseParameters>();

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
            Result<RequestObjectCreate.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Execute the request
            if (AlbumArtHash == null && AlbumArtFileSize == 0 && AlbumArtTnFileSize == 0)
                outboundQuery = WADM.RequestObjectCreate.BuildWithoutAlbumArt(this.UpdateID,
                    Artist, Album, Genre, Name, TrackNum, Year, MediaType, DMMCookie, Timeout, true);
            else if (!string.IsNullOrWhiteSpace(AlbumArtHash) && AlbumArtFileSize > 0 && AlbumArtTnFileSize > 0)
                outboundQuery = WADM.RequestObjectCreate.BuildWithAlbumArt(this.UpdateID,
                    Artist, Album, Genre, Name, TrackNum, Year, MediaType, DMMCookie, Timeout, AlbumArtHash, AlbumArtFileSize, AlbumArtTnFileSize, true);
            else
                result.FailMessage("An invalid combination of arguments was supplied!");

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the query
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery);

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
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestObjectCreate")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestObjectCreate.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestObjectCreate.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestObjectCreate.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

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
                return Result<RequestObjectCreate.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestObjectCreate.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestObjectCreate.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Attempts to create a new media file without album art.
        /// After this initial creation step, the file (and optional encrypted cover art) has to be uploaded via DeliveryClient within the timeout period.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Artist">The artist of the track.</param>
        /// <param name="Album">The album of the track.</param>
        /// <param name="Genre">The genre of the track according to the genre list.</param>
        /// <param name="Name">The title of the track.</param>
        /// <param name="TrackNum">The number of the track.</param>
        /// <param name="Year">The year that the track was from.</param>
        /// <param name="MediaType">The three letter file extension of the media file.</param>
        /// <param name="DMMCookie">The unknown DMMCookie (seems to be ignored).</param>
        /// <param name="Timeout">The upload timeout in seconds.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestObjectCreate.ResponseParameters> RequestObjectCreate(
            string Artist, string Album, string Genre, string Name, uint TrackNum, uint Year, string MediaType, uint DMMCookie, uint Timeout)
        {
            return RequestObjectCreate(Artist, Album, Genre, Name, TrackNum, Year, MediaType, DMMCookie, Timeout, null, 0, 0);
        }

        /// <summary>
        /// Attempts to update the metadata of a database item.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Field">Indicates what field of the track should be changed.</param>
        /// <param name="Index">The index of the track to be changed.</param>
        /// <param name="OriginalData">The original value of the field to be changed.</param>
        /// <param name="NewData">The new value of the field to be changed.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestObjectUpdate.ResponseParameters> RequestObjectUpdate(
            nxgmci.Protocol.WADM.RequestObjectUpdate.FieldType Field, uint Index, string NewData, string OriginalData = null)
        {
            // Create the result object
            Result<RequestObjectUpdate.ResponseParameters> result = new Result<RequestObjectUpdate.ResponseParameters>();

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
            Result<RequestObjectUpdate.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestObjectUpdate.Build(this.UpdateID, Field, Index, NewData, OriginalData), true);

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
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestObjectUpdate")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestObjectUpdate.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestObjectUpdate.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestObjectUpdate.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

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
                return Result<RequestObjectUpdate.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestObjectUpdate.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestObjectUpdate.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Attempts to update the metadata of a database item.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Field">Indicates what field of the track should be changed.</param>
        /// <param name="Index">The index of the track to be changed.</param>
        /// <param name="OriginalData">The original value of the field to be changed.</param>
        /// <param name="NewData">The new value of the field to be changed.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestObjectUpdate.ResponseParameters> RequestObjectUpdate(
            nxgmci.Protocol.WADM.RequestObjectUpdate.FieldType Field, uint Index, uint NewData, uint OriginalData = 0)
        {
            return RequestObjectUpdate(Field, Index, NewData.ToString(), OriginalData.ToString());
        }

        /// <summary>
        /// Attempts to delete a media file.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Index">The universal index of the media file to be deleted.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestObjectDestroy.ResponseParameters> RequestObjectDestroy(uint Index)
        {
            // Create the result object
            Result<RequestObjectDestroy.ResponseParameters> result = new Result<RequestObjectDestroy.ResponseParameters>();

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
            Result<RequestObjectDestroy.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestObjectDestroy.Build(this.UpdateID, Index), true);

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
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestObjectDestroy")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestObjectDestroy.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestObjectDestroy.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestObjectDestroy.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

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
                return Result<RequestObjectDestroy.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestObjectDestroy.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestObjectDestroy.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
        }

        /// <summary>
        /// Attempts to complete an upload. It must be polled until it is no longer busy.
        /// Using this request will update the client's update ID.
        /// </summary>
        /// <param name="Index">The index of the playlist or track.</param>
        /// <param name="Name">The title of the uploaded track.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public Result<RequestTransferComplete.ResponseParameters> RequestTransferComplete(uint Index, string Name)
        {
            // Create the result object
            Result<RequestTransferComplete.ResponseParameters> result = new Result<RequestTransferComplete.ResponseParameters>();

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
            Result<RequestTransferComplete.ResponseParameters> parseResult;

            // Create the event result object
            Result<Postmaster.QueryResponse> queryResult = new Result<Postmaster.QueryResponse>();

            // Allocate the shadow response text
            string shadowResponse = string.Empty;

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, WADM.RequestTransferComplete.Build(Index, Name), true);

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
                Result<Postmaster.QueryResponse>.SucceedProduct(queryResult, queryResponse, "RequestTransferComplete")));

            // Check, if the process failed
            if (result.Finalized)
                return result;

            // Parse the response
            parseResult = WADM.RequestTransferComplete.Parse(shadowResponse, validateInput, looseSyntax);

            // Sanity check the result
            if (parseResult == null)
                return Result<RequestTransferComplete.ResponseParameters>.FailMessage(result, "The parsed result was null!");
            if (parseResult.Success && (!parseResult.HasProduct || parseResult.Product == null))
                return Result<RequestTransferComplete.ResponseParameters>.FailMessage(result, "The parsed product was invalid!");

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
                return Result<RequestTransferComplete.ResponseParameters>.SucceedProduct(result, parseResult.Product, parseResult.Message);
            }

            // Try to return a detailed error
            if (parseResult.Error != null)
                return Result<RequestTransferComplete.ResponseParameters>.FailErrorMessage(result, parseResult.Error, "The parsing failed!");

            // If not possible, return simple failure
            return Result<RequestTransferComplete.ResponseParameters>.FailMessage(result, "The parsing failed due to an unknown reason!");
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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            outboundQuery = WADM.RequestRawData.Build(FromIndex, NumElem);

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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

            // Allocate the outbound query and shadow response text
            string outboundQuery = null, shadowResponse = string.Empty;

            // Build the query
            if (RequestPlayable)
                outboundQuery = WADM.RequestPlayableNavData.BuildPlayable(NodeID, NumElem, FromIndex);
            else
                outboundQuery = WADM.RequestPlayableNavData.BuildNav(NodeID, NumElem, FromIndex);

            // Verify the query
            if (string.IsNullOrWhiteSpace(outboundQuery))
                result.FailMessage("The query string could not be built since invalid parameters were supplied!");

            // Execute the request
            queryResponse = Postmaster.PostXML(ipEndpoint, Path, outboundQuery, true);

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
