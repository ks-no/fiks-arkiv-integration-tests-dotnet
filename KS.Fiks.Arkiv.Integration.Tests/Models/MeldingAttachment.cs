using System.IO;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Models;

public class MeldingAttachment
{
    public string Filename { get; set; }
    public FileStream Filestream { get; set; }
}