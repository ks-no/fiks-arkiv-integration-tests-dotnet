using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KS.Fiks.Arkiv.Integration.Tests.Helpers;
using KS.Fiks.Arkiv.Models.V1.Arkivstruktur;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using KS.Fiks.IO.Client.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Models;
using KS.FiksProtokollValidator.Tests.IntegrationTests.Validation;
using NUnit.Framework;

namespace KS.Fiks.Arkiv.Integration.Tests.Tests;

/// <summary>
/// Brukes til å teste utsending av en melding, og sjekker at det returneres riktige meldinger tilbake.
/// </summary>
public class TestHarness
{
    public Guid mottakerKontoId;
    public IntegrationTestsBase IntegrationTestsBase;
    public Guid messageId;
    public string meldingsType;
    public string serialized;
    public object payloadContent;
    public List<KeyValuePair<string, FileStream>>? attachments;
    public string? payloadFilename;
    public int expectedReturnMessagesCount;
    public bool expectedReturnMessagesCountAuto;
    public int maxWait;
    public IList<Action<MottattMeldingArgs>> expectedReturnMessages;
    public List<MottattMeldingArgs> MottatMeldingArgsList;
    private bool _sent;

    public TestHarness(
        IntegrationTestsBase integrationTestsBase,
        string meldingsType,
        object payloadContent,
        int? expectedReturnMessagesCount = null,
        List<KeyValuePair<string, FileStream>>? attachments = null,
        string? payloadFilename = null,
        Guid? mottakerKontoId = null,
        int maxWait = 10
    )
    {
        // TOOD: remove the reliance on the instance of IntegrationTestsBase here, we only need the base-config.
        IntegrationTestsBase = integrationTestsBase;
        MottatMeldingArgsList = new List<MottattMeldingArgs>();
        IntegrationTestsBase.onMottatMelding = (o, args) => MottatMeldingArgsList.Add(args);
        this.mottakerKontoId = mottakerKontoId ?? integrationTestsBase.MottakerKontoId;
        this.meldingsType = meldingsType;
        this.payloadContent = payloadContent;
        this.attachments = attachments;
        this.payloadFilename = payloadFilename;
        if (expectedReturnMessagesCount == null)
        {
            this.expectedReturnMessagesCountAuto = true;
        }

        this.expectedReturnMessagesCount = expectedReturnMessagesCount ?? 0;
        this.maxWait = maxWait;
        Assert.True(FiksArkivMeldingtype.IsGyldigProtokollType(meldingsType));
    }

    public TestHarness ExpectToFail(string? messageType = null)
    {
        if (string.IsNullOrEmpty(messageType))
        {
            ExpectReceiveOneOfTypes(messageType);
            return this;
        }

        ExpectReturnOfType(messageType);
        return this;
    }

    public TestHarness ExpectReturnOfType(string messageType)
    {
        if (expectedReturnMessagesCountAuto)
        {
            expectedReturnMessagesCount++;
        }

        Vent();
        IntegrationTestsBase.SjekkForventetMelding(messageId, messageType, MottatMeldingArgsList);
        return this;
    }

    public TestHarness ExpectReceiveOneOfTypes(params string[] messageType)
    {
        if (expectedReturnMessagesCountAuto)
        {
            expectedReturnMessagesCount++;
        }

        Vent();
        IntegrationTestsBase.SjekkForventetMelding(messageId, messageType, 1, MottatMeldingArgsList);
        return this;
    }

    public TestHarness ExpectReceiveAllOfTypes(params string[] messageType)
    {
        if (expectedReturnMessagesCountAuto)
        {
            expectedReturnMessagesCount += messageType.Length;
        }

        Vent();
        IntegrationTestsBase.SjekkForventetMelding(messageId, messageType, messageType.Length, MottatMeldingArgsList);
        return this;
    }

    public (MottattMeldingArgs?, PayloadFile?) GetMelding(string meldingsType)
    {
        var melding =
            IntegrationTestsBase.GetMottattMelding(MottatMeldingArgsList, messageId, meldingsType);
        Assert.IsNotNull(melding);

        var payload =
            MeldingHelper.GetDecryptedMessagePayload(melding).Result;
        Assert.IsNotNull(payload);
        Assert.IsNotEmpty(payload.PayloadAsString);
        return (melding, payload);

    }
    public (T?, MottattMeldingArgs?, PayloadFile?) GetMelding<T>(string meldingsType)
    {
        var (melding, payload) = GetMelding(meldingsType);
        var deserialized = SerializeHelper.DeserializeXml<T>(payload.PayloadAsString);
        return (deserialized, melding, payload);
    }

    public void Vent()
    {
        if (!_sent)
        {
            Send();
        }

        IntegrationTestsBase.VentPaSvar(expectedReturnMessagesCount, maxWait, MottatMeldingArgsList);
        Assert.GreaterOrEqual(MottatMeldingArgsList.Count, expectedReturnMessagesCount,
            "Fikk ikke forventet antall meldinger innen timeout");
    }

    public TestHarness Send()
    {
        serialized = SerializeHelper.Serialize(payloadContent);
        var validator = new SimpleXsdValidator();
        validator.Validate(serialized);
        messageId = IntegrationTestsBase.FiksRequestService.Send(
            mottakerKontoId,
            meldingsType,
            serialized,
            attachments,
            IntegrationTestsBase.TestSessionId,
            payloadFilename
        ).Result;
        _sent = true;
        Vent();
        return this;
    }
}