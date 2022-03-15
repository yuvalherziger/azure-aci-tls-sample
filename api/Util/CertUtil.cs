using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;

namespace api.Util;

public class CertUtil
{
    public CertUtil() { }

    public string cleanUpPemString(string pemStr) {
        string pattern = @"-+BEGIN[\w+\s+]+-+(.*)-+END[\w+\s+]+-+";
        return Regex.Replace(pemStr.Replace("\n", ""), pattern, match => {
            return match.Groups[1].Value.Trim('-');
        });
    }

    public byte[] readFile(string location) {
        FileStream f = new FileStream(location, FileMode.Open, FileAccess.Read);
        int size = (int) f.Length;
        byte[] fileData = new byte[size];
        size = f.Read(fileData, 0, size);
        f.Close();
        return fileData;
    }
 
    public X509Certificate2 attachPrivateKeyToTLSCert(X509Certificate2 certificate, string privateKeyStr) {
        byte[] pkBytes = Convert.FromBase64String(cleanUpPemString(privateKeyStr));
        using (RSA rsa = RSA.Create()) {
            rsa.ImportPkcs8PrivateKey(pkBytes, out _);
            using (X509Certificate2 pubOnly = certificate)
            using (X509Certificate2 pubPrivEphemeral = pubOnly.CopyWithPrivateKey(rsa)) {
                return new X509Certificate2(pubPrivEphemeral.Export(X509ContentType.Pfx));
            }
        }
    }
}