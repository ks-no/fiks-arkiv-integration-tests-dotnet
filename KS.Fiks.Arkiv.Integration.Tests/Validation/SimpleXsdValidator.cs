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
        public void Validate(string payload)
        {
            var xmlReaderSettings = new XmlReaderSettings();
            
            var arkivModelsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(assembly => assembly.GetName().Name == "KS.Fiks.Arkiv.Models.V1");
            
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmelding.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivmelding/v2",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/metadatakatalog/v2",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstruktur.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/arkivstruktur",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivmeldingOppdatering.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add(
                        "http://www.arkivverket.no/standarder/noark5/arkivmeldingoppdatering/v2", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.journalpostHent.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/journalpost/hent/v2",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.journalpostHentResultat.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add(
                        "http://www.arkivverket.no/standarder/noark5/journalpost/hent/resultat/v2", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.mappeHent.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("http://www.arkivverket.no/standarder/noark5/mappe/hent/v2",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.mappeHentResultat.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add(
                        "http://www.arkivverket.no/standarder/noark5/mappe/hent/resultat/v2", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.sokeresultatMinimum.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/sokeresultat/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstrukturMinimum.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/arkivstruktur/minimum/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.sokeresultatUtvidet.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("http://www.ks.no/standarder/fiks/arkiv/sokeresultat/v1",
                        schemaReader);
                }
            }
            
            var validationHandler = new ValidationHandler();
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.ValidationEventHandler += validationHandler.HandleValidationError;

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
    }
}