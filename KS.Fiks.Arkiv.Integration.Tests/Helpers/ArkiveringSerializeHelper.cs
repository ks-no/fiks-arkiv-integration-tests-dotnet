using System.IO;
using System.Text;
using System.Xml.Serialization;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Feilmelding;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;

namespace KS.FiksProtokollValidator.Tests.IntegrationTests.Helpers
{
    public class ArkiveringSerializeHelper
    {
        public static string Serialize(object arkivmelding)
        {
            var serializer = new XmlSerializer(arkivmelding.GetType());
            var stringWriter = new Utf8StringWriter();
            
            serializer.Serialize(stringWriter, arkivmelding);
            
            return stringWriter.ToString();
        }

        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        public static Arkivmelding? DeSerializeArkivmelding(string xml)
        {
            var serializer = new XmlSerializer(typeof(Arkivmelding));
            Arkivmelding? arkivmeldingDeserialized;
            using (TextReader reader = new StringReader(xml))
            {
                arkivmeldingDeserialized = (Arkivmelding) serializer.Deserialize(reader);
            }

            return arkivmeldingDeserialized;
        }
        
        public static ArkivmeldingKvittering DeSerializeArkivmeldingKvittering(string xml)
        {
            var serializer = new XmlSerializer(typeof(ArkivmeldingKvittering));
            ArkivmeldingKvittering arkivmeldingDeserialized;
            using (TextReader reader = new StringReader(xml))
            {
                arkivmeldingDeserialized = (ArkivmeldingKvittering) serializer.Deserialize(reader);
            }

            return arkivmeldingDeserialized;
        }
        
        public static T DeSerializeXml<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            T xmlDeserialized;
            using (TextReader reader = new StringReader(xml))
            {
                xmlDeserialized = (T) serializer.Deserialize(reader);
            }

            return xmlDeserialized;
        }
    }
}
