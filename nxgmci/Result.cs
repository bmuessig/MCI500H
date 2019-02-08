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
                    // There is no need to output the type name for vanilla exceptions
                    if (Error.GetType() != typeof(Exception) && !string.IsNullOrWhiteSpace(Error.Message))
                        return Error.Message;
                    else
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
            // Returns a custom success message
            if (Success && !string.IsNullOrWhiteSpace(successMessage))
                return string.Format("Success: {0}", successMessage);
            else if (!Success && Error != null)
            {
                // There is no need to output the type name for vanilla exceptions
                if (Error.GetType() != typeof(Exception) && !string.IsNullOrWhiteSpace(Error.Message))
                    return string.Format("Error ({0}): {1}", Error.GetType().ToString(), Error.Message);
                else if (!string.IsNullOrWhiteSpace(Error.Message))
                    return string.Format("Error: {0}", Error.Message);
                else
                    return string.Format("Error: {0}", Error.GetType().ToString());
            }

            // If no output message is available, return the default ToString()
            return base.ToString();
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
