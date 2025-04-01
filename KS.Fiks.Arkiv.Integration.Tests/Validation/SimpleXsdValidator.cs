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
            
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.arkivering.arkivmelding.opprett.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/arkivmelding/opprett/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.metadatakatalog.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/metadatakatalog/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstruktur.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/arkivstruktur/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.arkivering.arkivmelding.oppdater.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add(
                        "https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/arkivmelding/oppdater/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.journalpost.hent.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/journalpost/hent/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.journalpost.hent.resultat.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add(
                        "https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/journalpost/hent/resultat/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.mappe.hent.xsd"))
            {
                if (schemaStream != null)
                {
                    using XmlReader schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/mappe/hent/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.mappe.hent.resultat.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add(
                        "https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/mappe/hent/resultat/v1", schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.sok.resultat.minimum.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/sokeresultat/minimum/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.arkivstrukturMinimum.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/arkivstruktur/minimum/v1",
                        schemaReader);
                }
            }
            using (var schemaStream = arkivModelsAssembly?.GetManifestResourceStream("KS.Fiks.Arkiv.Models.V1.Schema.V1.no.ks.fiks.arkiv.v1.innsyn.sok.resultat.utvidet.xsd"))
            {
                if (schemaStream != null)
                {
                    using var schemaReader = XmlReader.Create(schemaStream);
                    xmlReaderSettings.Schemas.Add("https://ks-no.github.io/standarder/fiks-protokoll/fiks-arkiv/sokeresultat/utvidet/v1",
                        schemaReader);
                }
            }
            
            var validationHandler = new ValidationHandler();
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
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

                Assert.Fail($"Validering med xsd feilet: {string.Join(Environment.NewLine, validationHandler.errors)}");
            }
        }
    }
}