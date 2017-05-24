using System.Security.Cryptography;
using System.Text;

namespace Nop.Plugin.Payments.PagarMeCreditCard.Infrastructure
{
    public static class Sha1Cryptography
    {
        public static string CreateHash(string value)
        {
            SHA1 hasher = SHA1.Create();
            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] array = encoding.GetBytes(value);
            array = hasher.ComputeHash(array);

            StringBuilder strHexa = new StringBuilder();

            foreach (byte item in array)
            {
                strHexa.Append(item.ToString("x2"));
            }

            return strHexa.ToString();
        }
    }
}
