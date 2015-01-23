using System;
using System.Security.Cryptography;
using System.Text;

namespace RTextNppPlugin.Utilities
{
    public class HashUtilities
    {
        /**
         *
         * \brief   Hash an input string and return the hash as a 32 character hexadecimal string.
         *
         *
         * \param   input   The input.
         *
         * \return  The md 5 hash.
         */
        public static string getMd5Hash(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();

            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }

        /**
         *
         * \brief   Gets a GUID from a string.
         *
         *
         * \param   input   The input.
         *
         * \return  a new GUID
         */
        public static Guid getGUIDfromString(string input)
        {
            return new Guid(getMd5Hash(( input )));
        }
    }
}
