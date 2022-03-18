using System;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Validation
{
    public class SimpleXsdValidator
    {
        private string baseDirectory;
        
        public SimpleXsdValidator()
        {
            baseDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        } 
        
        public SimpleXsdValidator(string baseDirectory)
        {
            this.baseDirectory = baseDirectory;
        } 
        
        private void Validate(string payload, XmlReaderSettings xmlReaderSettings)
        {
            var validationHandler = new ValidationHandler();
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.ValidationEventHandler +=
                new ValidationEventHandler(validationHandler.HandleValidationError);

            var xmlReader = XmlReader.Create(new StringReader(payload), xmlReaderSettings);

            while (xmlReader.Read())
            {
            }

            if (validationHandler.HasErrors())
            {
                foreach (var error in validationHandler.errors)
                {
                    Console.Out.WriteLineAsync($"XSD validation error {error}");
                }
                Assert.Fail("Validering med xsd feilet");
            }
        }
        
        public void ValidateArkivmelding(string xml)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2",
                Path.Combine(baseDirectory, "Schema/arkivmelding.xsd"));
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine(baseDirectory, "Schema/metadatakatalog.xsd"));
            Validate(xml, xmlReaderSettings);
        }
        
        public void ValidateJournalpostHent(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/journalpost/hent/v2",
                Path.Combine(baseDirectory, "Schema/journalpostHent.xsd"));
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine(baseDirectory, "Schema/metadatakatalog.xsd"));
            Validate(payload, xmlReaderSettings);
        }
        
        public void ValidateArkivmeldingKvittering(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2",
                Path.Combine(baseDirectory, "Schema/arkivmelding.xsd"));
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine(baseDirectory, "Schema/metadatakatalog.xsd"));
            Validate(payload, xmlReaderSettings);
        }
        
        public void ValidateArkivmeldingSokeresultatMinimum(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/sokeresultat/v1",
                Path.Combine(baseDirectory, "Schema/sokeresultatMinimum.xsd"));
            xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/arkivstruktur/minimum/v1",
                Path.Combine(baseDirectory, "Schema/arkivstrukturMinimum.xsd"));
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine(baseDirectory, "Schema/metadatakatalog.xsd"));
            Validate(payload, xmlReaderSettings);
        }
        
        public void ValidateArkivmeldingSokeresultatNoekler(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/sokeresultat/v1",
                Path.Combine(baseDirectory, "Schema/sokeresultatNoekler.xsd"));
            xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/arkivstruktur/noekler/v1",
                Path.Combine(baseDirectory, "Schema/arkivstrukturNoekler.xsd"));
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine(baseDirectory, "Schema/metadatakatalog.xsd"));
            Validate(payload, xmlReaderSettings);
        }

        public void ValidateArkivmeldingSokeresultatUtvidet(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/sokeresultat/v1",
                Path.Combine(baseDirectory,"Schema/sokeresultatUtvidet.xsd"));
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivstruktur",
                Path.Combine(baseDirectory, "Schema/arkivstruktur.xsd"));   
            xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                Path.Combine(baseDirectory, "Schema/metadatakatalog.xsd"));
            Validate(payload, xmlReaderSettings);
        }
    }
}