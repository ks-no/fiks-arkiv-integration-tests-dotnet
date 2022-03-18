using System.ComponentModel.DataAnnotations;
using System.IO;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Models
{
    public class PayloadFile
    {
        public string Filename { get; set; }

        public Stream PayloadStream { get; set; }
        public string  PayloadAsString { get; set; }
    }
}
