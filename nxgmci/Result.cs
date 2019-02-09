using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci
{
    public class Result<T>
    {
        /// <summary>
        /// Indicates whether the result is final and cannot be changed.
        /// </summary>
        public bool Finalized { get; private set; }

        /// <summary>
        /// Indicates a successful result or failure if not true.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Indicates whether the result contains an associated product.
        /// </summary>
        public bool HasProduct { get; private set; }

        /// <summary>
        /// Stores the product of a positive result.
        /// </summary>
        public T Product { get; private set; }
        
        /// <summary>
        /// Stores the error that prevented a positive outcome.
        /// </summary>
        public Exception Error
        {
            get;
            private set;
        }

        /// <summary>
        /// Internal success message string.
        /// </summary>
        private string successMessage;

        /// <summary>
        /// Returns the success or error message associated with the result.
        /// A custom string has preference over an exception.
        /// </summary>
        public string Message
        {
            get
            {
                // Returns a custom success message
                if (Success && !string.IsNullOrWhiteSpace(successMessage))
                    return successMessage;
                else if (!Success && Error != null)
                {
                    // If a message is available, return that one
                    if (!string.IsNullOrWhiteSpace(Error.Message))
                        return Error.Message;
                    else // Otherwise, the type might be useful in some cases
                        return Error.GetType().ToString();
                }

                // If no output message is available, an empty string is returned
                return string.Empty;
            }
        }

        // For debugging this will also do performance testing

        /// <summary>
        /// Stores the time that the result object was created.
        /// </summary>
        public DateTime TimeCreated { get; private set; }

        /// <summary>
        /// Stores the time that the result object was finalized.
        /// </summary>
        public DateTime TimeFinalized { get; private set; }

        /// <summary>
        /// Returns the time that passed between the result object creation and finalization.
        /// </summary>
        public TimeSpan TimeDelta
        {
            get
            {
                // If the result is finalized, we can return a timespan
                if (Finalized)
                    return (TimeFinalized - TimeCreated);

                // Otherwise return a zero time span
                return new TimeSpan(0);
            }
        }

        /// <summary>
        /// Default constructor. Stores the time of object creation.
        /// </summary>
        public Result()
        {
            this.TimeCreated = DateTime.Now;
            this.Finalized = false;
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string representation of the object.</returns>
        public override string ToString()
        {
            return ToString(true, true, true);
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <param name="PrintRecursive">If true, a recusive, line-by-line list of all errors is returned.</param>
        /// <param name="PrintPrefix">
        /// If true, in recursive mode, the presence of an error is indicated.
        /// If true, in non-recursive mode or on success, an 'Error:' or 'Success:' prefix is prepended.
        /// </param>
        /// <param name="PrintLevels">If true, the relative level of the error is prepended.</param>
        /// <returns>A string representation of the object.</returns>
        public string ToString(bool PrintRecursive, bool PrintPrefix = true, bool PrintLevels = true)
        {
            // Returns a custom success message
            if (Success && !string.IsNullOrWhiteSpace(successMessage))
                return string.Format(PrintPrefix ? "Success: {0}" : "{0}", successMessage);
            else if (Success) // Success, without more info - the norm
                return "Success";
            else if (!Success && Error != null)
            {
                // Since stack recursion is undesireable, this stores the current inner exception
                Exception error = Error;

                // This is used to build the output string
                StringBuilder outputBuilder = new StringBuilder();

                // This stores whether a new line is required
                bool requireNewline = false;

                // If desired, print a recursive error description
                if (PrintRecursive && PrintPrefix)
                {
                    outputBuilder.Append("One or multiple errors occured:");
                    requireNewline = true;
                }

                // Loop through all levels of nesting and exit early, if only one level shall be returned
                for (int level = 0; PrintRecursive || (level < 1 && !PrintRecursive); level++)
                {
                    // If a newline is required print it and reset the flag
                    if (requireNewline)
                    {
                        outputBuilder.AppendLine();
                        requireNewline = false;
                    }

                    // Generate the error line string
                    string errorString = ErrorToString(error, !PrintRecursive && PrintPrefix);

                    // Check, if the error string is empty
                    if (!string.IsNullOrWhiteSpace(errorString))
                    {
                        // If desired, prepend the error level
                        if (PrintLevels)
                        {
                            // Append the relative level
                            for (int marker = 0; marker < level; marker++)
                                outputBuilder.Append('>');

                            // Append a space for readability
                            outputBuilder.Append(' ');
                        }

                        // Append the string
                        outputBuilder.Append(errorString);
                        requireNewline = true;
                    }

                    // Assign the current to the inner exception
                    error = error.InnerException;

                    // Check if this is the final level
                    if (error == null)
                        break;
                }

                // Finally, return the result, if it is not empty
                if (outputBuilder.Length > 0)
                    return string.Copy(outputBuilder.ToString());
            }
            else if (!Success) // An error occured and there is no more info
                return "Error";

            // If no output message is available, return the default ToString()
            return base.ToString();
        }

        /// <summary>
        /// Returns the matching message string to describe an exception.
        /// </summary>
        /// <param name="Error">The exception to print.</param>
        /// <param name="UseErrorPrefix">Indicates whether to prefix the output with 'Error:'.</param>
        /// <returns>A string that describes the exception.</returns>
        private string ErrorToString(Exception Error, bool UseErrorPrefix)
        {
            // Sanity checking
            if (Error == null)
                return string.Empty;

            // There is no need to output the type name for vanilla exceptions
            if (Error.GetType() != typeof(Exception) && !string.IsNullOrWhiteSpace(Error.Message)) // There is a type and a message
                return string.Format(UseErrorPrefix ? "Error ({0}): {1}" : "{0}: {1}", Error.GetType().ToString(), Error.Message);
            else if (!string.IsNullOrWhiteSpace(Error.Message)) // There is a message and this is the default Exception type
                return string.Format(UseErrorPrefix ? "Error: {0}" : "{0}", Error.Message);
            else // There is only a type
                return string.Format(UseErrorPrefix ? "Error: {0}" : "{0}", Error.GetType().ToString());
        }

        /// <summary>
        /// Finalizes the result set to success with only a message and returns itself.
        /// </summary>
        /// <param name="Message">A custom success message that could be shown to the user. This can optionally be string.Format formatted.</param>
        /// <param name="Arguments">Optional string.Format arguments.</param>
        /// <returns>Itself to ease return statements.</returns>
        public Result<T> SucceedMessage(string Message, params object[] Arguments)
        {
            // Make sure that the class is not already finalized
            if (this.Finalized)
                throw new InvalidOperationException("A finalized result cannot be edited!");

            // Finalize itself
            this.Finalized = true;

            // Store the time of finalization
            this.TimeFinalized = DateTime.Now;

            // Attempt to format the message (if possible)
            if (!string.IsNullOrWhiteSpace(Message) && Arguments != null)
            {
                if (Arguments.Length > 0)
                {
                    try
                    {
                        Message = string.Format(Message, Arguments);
                    }
                    catch (Exception)
                    { }
                }
            }

            // Assign the values
            this.Success = true;
            this.successMessage = Message;
            this.HasProduct = false;
            this.Error = null;

            // Returning itself allows simple return statements
            return this;
        }

        /// <summary>
        /// Finalizes the result set to success, stores the product and returns itself.
        /// </summary>
        /// <param name="Product">The product generated by the operation the result is emitted by.</param>
        /// <returns>Itself to ease return statements.</returns>
        public Result<T> Succeed(T Product, string Message = null)
        {
            // Make sure that the class is not already finalized
            if (this.Finalized)
                throw new InvalidOperationException("A finalized result cannot be edited!");

            // Finalize itself
            this.Finalized = true;

            // Store the time of finalization
            this.TimeFinalized = DateTime.Now;

            // Store the product
            this.Product = Product;

            // Assign the values
            this.Success = true;
            this.successMessage = Message;
            this.HasProduct = true;
            this.Error = null;

            // Returning itself allows simple return statements
            return this;
        }

        /// <summary>
        /// Finalizes the result set to an error with only a message and returns itself.
        /// </summary>
        /// <param name="Message">A custom error message that could be shown to the user. This can optionally be string.Format formatted.</param>
        /// <param name="Arguments">Optional string.Format arguments.</param>
        /// <returns>Itself to ease return statements.</returns>
        public Result<T> FailMessage(string Message, params object[] Arguments)
        {
            // Attempt to format the message (if possible)
            if (!string.IsNullOrWhiteSpace(Message) && Arguments != null)
            {
                if (Arguments.Length > 0)
                {
                    try
                    {
                        Message = string.Format(Message, Arguments);
                    }
                    catch (Exception)
                    { }
                }
            }

            return Fail(Message, null);
        }

        /// <summary>
        /// Finalizes the result set to an error and returns itself.
        /// </summary>
        /// <param name="Error">An exception that was thrown preventing a successful result.</param>
        /// <returns>Itself to ease return statements.</returns>
        public Result<T> Fail(Exception Error)
        {
            return Fail(null, Error);
        }

        /// <summary>
        /// Finalizes the result set to an error and returns itself.
        /// </summary>
        /// <param name="Message">A custom error message that could be shown to the user.</param>
        /// <param name="Error">An exception that was thrown preventing a successful result.</param>
        /// <returns>Itself to ease return statements.</returns>
        public Result<T> Fail(string Message, Exception Error)
        {
            // Make sure that the class is not already finalized
            if (this.Finalized)
                throw new InvalidOperationException("A finalized result cannot be edited!");

            // Finalize itself
            this.Finalized = true;

            // Store the time of finalization
            this.TimeFinalized = DateTime.Now;

            // Assign the values
            this.Success = false;
            this.HasProduct = false;

            // Nest the exception if possible
            if (!string.IsNullOrWhiteSpace(Message))
                this.Error = new Exception(Message, Error);
            else
                this.Error = Error;

            // Returning itself allows simple return statements
            return this;
        }
    }
}
