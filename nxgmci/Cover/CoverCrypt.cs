using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using nxgmci.Properties;

namespace nxgmci.Cover
{
    /// <summary>
    /// This class provides static functions to encode/encrypt as well as to decode/decrypt album art.
    /// </summary>
    public static class CoverCrypt
    {
        // Local, static variables
        private static byte[] cryptoKey;
        private readonly static uint cryptoLength = 0x1000;
        private readonly static byte[] coverOriginal = Resources.CoverOriginal;
        private readonly static byte[] coverEncoded = Resources.CoverEncoded;

        // DISCLAIMER:
        // This file nor this project contains the actual private key encoded or in plain
        // The key is used to encode the cover art that is stored on the stereo
        // The purpose of this class is to calculate the key based of commonly available data
        // In this case, the album art of an MP3 uploaded via WADM is used to recover the key
        // This works since the encoded file is equal to the original one XOR the key
        // We can recover the key by performing encoded byte XOR original byte
        // WADM stores the original, scaled album art in %TEMP%, so it provides all data on it's own
        // The encoded album art was retrieved from the stereo's hard disk
        // To determine the key length, many approaches can be taken
        // Here, it was enough to observe the wrapping behavoir after 0x1000 bytes

        /// <summary>
        /// Returns the length of the crypto key in bytes.
        /// </summary>
        public static uint CryptoLength
        {
            get
            {
                // Public key-length getter
                return cryptoLength;
            }
        }

        /// <summary>
        /// Returns the current crypto key. If is not yet calculated, calculates it.
        /// </summary>
        /// <returns>The crypto key.</returns>
        public static byte[] GetCryptoKey()
        {
            // Check, if the crypto key has already been calculated and if it's of correct length
            if (VerifyCryptoKey(cryptoKey, true))
                if (!CalculateCryptoKey())
                    return null;
            
            // Return the existing crypto key
            return cryptoKey;
        }

        /// <summary>
        /// Calculates the crypto key and overwrites the current one. This can be used to pre-calculate the key ahead of time.
        /// </summary>
        /// <returns>True, if the key could be calculated and set successfully. False on error.</returns>
        public static bool CalculateCryptoKey()
        {
            // Check if our resource files are present
            if (coverOriginal == null || coverEncoded == null)
                return false;

            // Check if the resource files are big enough to extract the full key
            if (coverOriginal.Length < cryptoLength || coverEncoded.Length < cryptoLength)
                return false;

            // Allocate memory for the key
            byte[] localCryptoKey = new byte[cryptoLength];

            // Now, the fun begins
            // We extract the key by XOR'ing the original with the encoded data
            for (int offset = 0; offset < cryptoLength; offset++)
                localCryptoKey[offset] = (byte)((coverEncoded[offset] ^ coverOriginal[offset]) & 0xFF);

            // Finally, we verify the new key
            if (!VerifyCryptoKey(localCryptoKey, false))
                return false;

            // And assign it
            cryptoKey = localCryptoKey;

            // Return success
            return true;
        }

        /// <summary>
        /// Sets the current crypto key to a user supplied one and checks the key's validity.
        /// </summary>
        /// <param name="Key">The new crypto key to be used.</param>
        /// <returns>True if the new crypto key is valid and was successfully set. False, if the key was invalid.</returns>
        public static bool SetCryptoKey(byte[] Key)
        {
            // Check the user supplied key
            if (VerifyCryptoKey(Key, false))
            {
                cryptoKey = Key;
                return true;
            }

            // We fall through, into darkness
            return false;
        }

        /// <summary>
        /// Verifies a supplied crypto key.
        /// </summary>
        /// <param name="Key">The key to be verified.</param>
        /// <param name="QuickCheck">If false, loop over the key's data and check it's validity. If true, only check the size.</param>
        /// <returns>True if the key is valid and false, if it's not.</returns>
        public static bool VerifyCryptoKey(byte[] Key, bool QuickCheck)
        {
            // Input sanity checks
            if (Key == null)
                return false;

            // Make sure the length is correct
            if (Key.Length != cryptoLength)
                return false;

            // There are no null bytes in a valid key. They are forbidden!
            // For a quick check, we skip this
            if (!QuickCheck)
            {
                foreach (byte b in Key)
                    if (b == 0)
                        return false;
            }

            // Return success
            return true;
        }

        /// <summary>
        /// Applies the key to a buffer.
        /// </summary>
        /// <param name="Buffer">The buffer to be modified.</param>
        /// <param name="Length">The number of bytes to be modified.</param>
        /// <param name="Offset">The maximum number of bytes to skip before starting to modify data.</param>
        /// <returns>True if the key could be applied to the buffer successfully. False if the data was not changed and there had been an error.</returns>
        public static bool EncryptBuffer(ref byte[] Buffer, uint Length, uint Offset = 0)
        {
            // Make sure we have a valid key
            if (!VerifyCryptoKey(cryptoKey, true))
                return false;

            // Also perform sanity checks on the input
            if (Buffer == null)
                return false;

            // Clip the length to be at most the size of the buffer
            if (Length + Offset > Buffer.Length)
                Length = (uint)(Buffer.Length - (int)Offset);

            // If everything matches up, we apply the encryption
            for (uint ptr = Offset; ptr < Offset + Length; ptr++)
                Buffer[ptr] ^= cryptoKey[(ptr - Offset) % cryptoLength];

            // And we return success
            return true;
        }

        /// <summary>
        /// Applies the key to an input stream and writes the result to an output stream.
        /// </summary>
        /// <param name="InputStream">The stream to read the input data from.</param>
        /// <param name="OutputStream">The stream to write the output data to.</param>
        /// <param name="Length">Maximum number of bytes to read. If zero, read until EOF.</param>
        /// <returns>Returns the number of bytes written successfully.</returns>
        public static uint EncryptStream(Stream InputStream, Stream OutputStream, uint Length = 0)
        {
            // Make sure we have a valid key
            if (!VerifyCryptoKey(cryptoKey, true))
                return 0;

            // Also perform sanity checks on the input
            if (InputStream == null || OutputStream == null)
                return 0;
            if (!InputStream.CanRead || !OutputStream.CanWrite)
                return 0;

            // This stores the number of bytes read
            uint count = 0;

            // Loop over the input data
            try
            {
                for (; (count < Length) || (Length == 0); count++)
                {
                    // Read a new byte from the input stream
                    int b = InputStream.ReadByte();

                    // Check, if we have reached the end of the input stream
                    if (b == -1)
                        break;

                    // Now, encode the byte and write it to the output
                    OutputStream.WriteByte((byte)(b ^ cryptoKey[count % cryptoLength]));
                }
            }
            catch (Exception)
            {
                // On error, return the number of bytes read so far
                return count;
            }

            // Finally, return the number of bytes written
            return count;
        }
    }
}
