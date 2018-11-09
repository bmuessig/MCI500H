using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nxgmci.Properties;

namespace nxgmci.Cover
{
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

        public static uint CryptoLength
        {
            get
            {
                // Public key-length getter
                return cryptoLength;
            }
        }

        public static byte[] GetCryptoKey()
        {
            // Check, if the crypto key has already been calculated and if it's of correct length
            if (VerifyCryptoKey(cryptoKey, true))
                if (!CalculateCryptoKey())
                    return null;
            
            // Return the existing crypto key
            return cryptoKey;
        }

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

        public static bool SetCryptoKey(byte[] Key)
        {
            // Check the user supplied key
            if (VerifyCryptoKey(Key, true))
            {
                cryptoKey = Key;
                return true;
            }

            // We fall through, into darkness
            return false;
        }

        public static bool VerifyCryptoKey(byte[] Key, bool QuickScan)
        {
            // Input sanity checks
            if (Key == null)
                return false;

            // Make sure the length is correct
            if (Key.Length != cryptoLength)
                return false;

            // There are no null bytes in a valid key. They are forbidden!
            foreach (byte b in Key)
                if (b == 0)
                    return false;

            // Return success
            return true;
        }

        public static bool EncryptBuffer(ref byte[] Buffer, uint Length, uint Offset = 0)
        {
            // Make sure we have a valid key
            if (!VerifyCryptoKey(cryptoKey, true))
                return false;

            // Also perform sanity checks on the input
            if (Buffer == null)
                return false;
            if (Length + Offset > Buffer.Length)
                return false;

            // If everything matches up, we apply the encryption
            for (uint ptr = Offset; ptr < Offset + Length; ptr++)
                Buffer[ptr] ^= cryptoKey[(ptr - Offset) % cryptoLength];

            // And we return success
            return true;
        }
    }
}
