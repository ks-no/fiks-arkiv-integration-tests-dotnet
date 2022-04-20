using System;
using System.IO;
using System.Linq;
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
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmelding.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2", schemaReader);
                }
            }
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            Validate(xml, xmlReaderSettings);
        }
        
        public void ValidateArkivmeldingOppdatering(string xml)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmelding.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2", schemaReader);
                }
            }
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmeldingOppdatering.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivmeldingoppdatering/v2", schemaReader);
                }
            }
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            Validate(xml, xmlReaderSettings);
        }
        
        public void ValidateJournalpostHent(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.journalpostHent.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/journalpost/hent/v2", schemaReader);
                }
            }
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            Validate(payload, xmlReaderSettings);
        }
        
        public void ValidateJournalpostHentResultat(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmelding.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.journalpostHentResultat.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/journalpost/hent/resultat/v2", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            Validate(payload, xmlReaderSettings);
        }
        
        public void ValidateArkivmeldingKvittering(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmelding.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            Validate(payload, xmlReaderSettings);
        }
        
        public void ValidateArkivmeldingSokeresultatMinimum(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.sokeresultatMinimum.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/sokeresultat/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstrukturMinimum.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/arkivstruktur/minimum/v1", schemaReader);
                }
            }
            using (Stream schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd")) {
                using (XmlReader schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            Validate(payload, xmlReaderSettings);
        }
        
        public void ValidateArkivmeldingSokeresultatNoekler(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.sokeresultatNoekler.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/sokeresultat/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstrukturNoekler.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/arkivstruktur/noekler/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            Validate(payload, xmlReaderSettings);
        }

        public void ValidateArkivmeldingSokeresultatUtvidet(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.sokeresultatUtvidet.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/sokeresultat/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstruktur.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivstruktur", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd")) {
                using (var schemaReader = XmlReader.Create(schemaStream)) {
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2", schemaReader);
                }
            }
            Validate(payload, xmlReaderSettings);
        }
    }
}