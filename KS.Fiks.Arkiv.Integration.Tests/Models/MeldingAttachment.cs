using System;
using System.IO;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Models;

public class MeldingAttachment : IDisposable
{
    public string Filename { get; set; }
    public FileStream Filestream { get; set; }

    public void Dispose()
    {
        Filestream?.Dispose();
    }
}