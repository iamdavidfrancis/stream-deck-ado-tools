using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;

namespace StreamDeckAzureDevOps.Utilities
{
    internal static class HttpUtilities
    {
        public static AuthenticationHeaderValue GetBasicAuthHeader(string username, string password)
        {
            var authString = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", username, password);
            var encodedAuthString = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(authString));

            return new AuthenticationHeaderValue("Basic", encodedAuthString);
        }
    }
}
