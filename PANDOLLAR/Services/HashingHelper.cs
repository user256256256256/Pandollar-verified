using System;

namespace PANDOLLAR.Services
{
    // A utility class for encoding and decoding GUIDs and strings
    public class HashingHelper
    {
        // Encodes a GUID into a URL-safe Base64 string
        public static string EncodeGuidID(Guid value)
        {
            // Convert the GUID to a byte array
            var bytes = value.ToByteArray();
            // Convert the byte array to a Base64 string and replace URL-unsafe characters
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_'); // URL Safe
        }

        // Decodes a URL-safe Base64 string back into a GUID
        public static Guid DecodeGuidID(string encoded)
        {
            // Replace URL-safe characters back to Base64 original characters
            var base64 = encoded.Replace('-', '+').Replace('_', '/');
            // Convert the Base64 string back to a byte array
            var bytes = Convert.FromBase64String(base64);
            // Convert the byte array back to a GUID
            return new Guid(bytes);
        }

        // Encodes a string into a URL-safe Base64 string
        public static string EncodeString(string value)
        {
            // Convert the string to a byte array using UTF-8 encoding
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            // Convert the byte array to a Base64 string and replace URL-unsafe characters
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_'); // URL Safe
        }

        // Decodes a URL-safe Base64 string back into a regular string
        public static string DecodeString(string encoded)
        {
            // Replace URL-safe characters back to Base64 original characters
            var base64 = encoded.Replace('-', '+').Replace('_', '/');
            // Convert the Base64 string back to a byte array
            var bytes = Convert.FromBase64String(base64);
            // Convert the byte array back to a string using UTF-8 encoding
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}

