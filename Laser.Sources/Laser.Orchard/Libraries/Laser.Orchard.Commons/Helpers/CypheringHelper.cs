﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Laser.Orchard.Commons.Helpers {
    public static class CypheringHelper {

        /// <summary>
        /// returns base64 string of message hashed with sha512 algorithm
        /// </summary>
        /// <param name="message">the message to hash</param>
        /// <param name="key">the key to use</param>
        /// <returns></returns>
        public static string HMACSHA512(this string message, string key) {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(key);

            HMACSHA512 hmacsha256 = new HMACSHA512(keyByte);
            byte[] messageBytes = encoding.GetBytes(message);

            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashmessage);
            //         return (ByteToString(hashmessage));
        }

        //        public static string AesCypher(this string message, string key){
        //        string original = "Here is some data to encrypt!";

        //                // Create a new instance of the Aes
        //                // class.  This generates a new key and initialization 
        //                // vector (IV).
        //                using (Aes myAes = Aes.Create())
        //                {

        //                    // Encrypt the string to an array of bytes.
        //                    byte[] encrypted = EncryptStringToBytes_Aes(original, 
        //myAes.Key, myAes.IV);

        //                    // Decrypt the bytes to a string.
        //                    string roundtrip = DecryptStringFromBytes_Aes(encrypted, 
        //myAes.Key, myAes.IV);

        //                    //Display the original data and the decrypted data.
        //                    Console.WriteLine("Original:   {0}", original);
        //                    Console.WriteLine("Round Trip: {0}", roundtrip);
        //                }}

    }
}
