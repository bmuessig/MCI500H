using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This API call allows updating the metadata of a database item.
    /// 
    /// NOTE: One thing that always seemed strange to me is why the stereo needs to much babysitting from WADM.
    /// It is perfectly capable of extracting meta data of verious file formats, as well as extracting and scaling cover art.
    /// I believe that it can even do meta data editing and it might actually alter the original media file as a result of this API call.
    /// Why do I have to re-implement all this stuff on the client side and send it over, if it could do it on it's own and even allow editing?!
    /// </summary>
    public static class RequestObjectUpdate
    {
        // RequestObjectUpdate Parser
        private readonly static WADMParser parser = new WADMParser("requestplaylistdelete", "responseparameters", false);

        /// <summary>
        /// Assembles a RequestObjectUpdate request to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The modification update ID passed as a token.</param>
        /// <param name="Field">Indicates what field of the track should be changed.</param>
        /// <param name="Index">The index of the track to be changed.</param>
        /// <param name="OriginalData">The original value of the field to be changed.</param>
        /// <param name="NewData">The new value of the field to be changed.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build(uint UpdateID, FieldType Field, uint Index, string NewData, string OriginalData = null)
        {
            // Convert the field to it's internal string representation
            string rawField = GetFieldString(Field);

            // Sanity check the input
            if (Index == 0 || string.IsNullOrWhiteSpace(rawField))
                return null;

            // Check, if any fields are invalid and need to be replaced
            // Note, that these strings should better be localized
            // TODO: Check, how the old program did it and if it sent empty strings or the placeholder text
            if (string.IsNullOrWhiteSpace(NewData))
                switch (Field)
                {
                    case FieldType.Artist:
                        NewData = "No Artist";
                        break;
                    case FieldType.Album:
                        NewData = "No Album";
                        break;
                    case FieldType.Genre:
                        NewData = "No Genre";
                        break;
                    case FieldType.Name:
                        return null;
                    default:
                        NewData = string.Empty;
                        break;
                }

            // Normalize the names
            if (OriginalData == null)
                OriginalData = string.Empty;

            // And build the request
            return string.Format(
                "<requestplaylistdelete><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<index>{1}</index>" +
                "<field>{2}</field>" +
                "<originaldata>{3}</originaldata>" +
                "<newdata>{4}</newdata>" +
                "</requestparameters></requestplaylistdelete>",
                UpdateID,
                Index,
                WADMParser.TrimValue(WADMParser.EncodeValue(rawField), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(OriginalData), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(NewData), true));
        }

        /// <summary>
        /// Indicates what field of the track should be changed.
        /// </summary>
        public enum FieldType : byte
        {
            /// <summary>
            /// Selects the artist field as the field to be changed.
            /// </summary>
            Artist,

            /// <summary>
            /// Selects the album field as the field to be changed.
            /// </summary>
            Album,
            
            /// <summary>
            /// Selects the genre field as the field to be changed.
            /// </summary>
            Genre,
            
            /// <summary>
            /// Selects the title field as the field to be changed.
            /// </summary>
            Name,
            
            /// <summary>
            /// Selects the track number field as the field to be changed.
            /// </summary>
            TrackNum,

            /// <summary>
            /// Selects the year field as the field to be changed.
            /// </summary>
            Year
        }

        /// <summary>
        /// Returns the internal string representation of a field type.
        /// </summary>
        /// <param name="Field"></param>
        /// <returns></returns>
        private static string GetFieldString(FieldType Field)
        {
            switch (Field)
            {
                case FieldType.Artist:
                    return "artist";
                case FieldType.Album:
                    return "album";
                case FieldType.Genre:
                    return "genre";
                case FieldType.Name:
                    return "name";
                case FieldType.TrackNum:
                    return "trackno";
                case FieldType.Year:
                    return "year";
            }

            return null;
        }

        /// <summary>
        /// Parses RequestObjectUpdate's ResponseParameters and returns the result.
        /// </summary>
        /// <param name="Response">The response received from the stereo.</param>
        /// <param name="ValidateInput">Indicates whether to validate the data values received.</param>
        /// <param name="LazySyntax">Indicates whether to ignore minor syntax errors.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public static Result<ResponseParameters> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // Allocate the result object
            Result<ResponseParameters> result = new Result<ResponseParameters>();

            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return Result<ResponseParameters>.FailMessage(result, "The response may not be null!");

            // Then, parse the response
            Result<WADMProduct> parserResult = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!parserResult.Success)
                if (parserResult.Error != null)
                    return Result<ResponseParameters>.FailErrorMessage(result, parserResult.Error, "The parsing failed!");
                else
                    return Result<ResponseParameters>.FailMessage(result, "The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (parserResult.Product == null)
                return Result<ResponseParameters>.FailMessage(result, "The parsing product was null!");

            // And also make sure our state is correct
            if (parserResult.Product.Elements == null)
                return Result<ResponseParameters>.FailMessage(result, "The list of parsed elements is null!");

            // Try to parse the status
            Result<WADMStatus> statusResult = WADMStatus.Parse(parserResult.Product.Elements, ValidateInput);

            // Check if it failed
            if (!statusResult.Success)
                if (statusResult.Error != null)
                    return Result<ResponseParameters>.FailErrorMessage(result, statusResult.Error, "The status code parsing failed!");
                else
                    return Result<ResponseParameters>.FailMessage(result, "The status code parsing failed for unknown reasons!");

            // Make sure the product is there
            if (statusResult.Product == null)
                return Result<ResponseParameters>.FailMessage(result, "The status code parsing product was null!");

            // Now, make sure our mandatory arguments exist
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "updateid");
            if (!parserResult.Product.Elements.ContainsKey("index"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "index");

            // Then, try to parse the parameters
            uint updateID, index;

            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "updateid");
            if (!uint.TryParse(parserResult.Product.Elements["index"], out index))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "index");

            // Finally, return the response
            return Result<ResponseParameters>.SucceedProduct(result, new ResponseParameters(statusResult.Product, updateID, index));
        }

        /// <summary>
        /// RequestObjectUpdate's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// The status code returned for the query.
            /// </summary>
            public readonly WADMStatus Status;

            /// <summary>
            /// The modification update ID passed as a token. Equal to the originally supplied update ID + 1.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// The index of the track edited.
            /// </summary>
            public readonly uint Index;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="Status">The status code returned for the query.</param>
            /// <param name="UpdateID">The modification update ID passed as a token. Equal to the originally supplied update ID + 1.</param>
            /// <param name="Index">The index of the playlist or track deleted.</param>
            internal ResponseParameters(WADMStatus Status, uint UpdateID, uint Index)
            {
                // Sanity check the input
                if (Status == null)
                    throw new ArgumentNullException("Status");

                this.Status = Status;
                this.UpdateID = UpdateID;
                this.Index = Index;
            }
        }
    }
}
