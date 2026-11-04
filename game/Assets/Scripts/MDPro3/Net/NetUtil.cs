using System;
using UnityEngine.Networking;

namespace MDPro3.Net
{
    public static class NetUtil
    {

        public static bool IsValidUrl(string inputUrl)
        {
            if(string.IsNullOrEmpty(inputUrl))
                return false;

            if (!inputUrl.StartsWith("http://") && !inputUrl.StartsWith("https://"))
                inputUrl = "http://" + inputUrl;

            if (!Uri.TryCreate(inputUrl, UriKind.Absolute, out Uri uriResult))
                return false;

            if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
                return false;

            return true;
        }

        public static bool IsValidDownloadUrl(string inputUrl, string[] supportExtensions)
        {
            bool hasValidExtension = false;
            foreach(var ext in supportExtensions)
                if (inputUrl.ToLower().Contains(ext.ToLower()))
                {
                    hasValidExtension = true;
                    break;
                }
            if (hasValidExtension)
            {
                return IsValidUrl(inputUrl);
            }
            else
                return false;
        }

        public class AcceptAllCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }

    }
}
