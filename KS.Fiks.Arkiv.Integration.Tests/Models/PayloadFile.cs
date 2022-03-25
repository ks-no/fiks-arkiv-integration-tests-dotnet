using System.ComponentModel.DataAnnotations;
using System.IO;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Models
{
    public class PayloadFile
    {
        public PayloadFile(string filename, string payloadAsString)
        {
            Filename = filename;
            PayloadAsString = payloadAsString;
        }

        public string Filename { get; set; }
        public string  PayloadAsString { get; set; }
    }
}
