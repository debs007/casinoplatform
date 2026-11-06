using UnityEngine.Networking;
using System.Security.Cryptography.X509Certificates;

namespace TwelveCards
{
    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Always accept
            return true;
        }
    }
}
