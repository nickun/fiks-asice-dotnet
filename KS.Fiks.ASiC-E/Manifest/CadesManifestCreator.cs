using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using KS.Fiks.ASiC_E.Model;
using KS.Fiks.ASiC_E.Xsd;

namespace KS.Fiks.ASiC_E.Manifest
{
    public class CadesManifestCreator : AbstractManifestCreator
    {
        public const string FILENAME = "META-INF/asicmanifest.xml";
        private static readonly Encoding Encoding = Encoding.UTF8;

        public override ManifestContainer CreateManifest(IEnumerable<AsicePackageEntry> entries)
        {
            var signatureFileRef = CreateSignatureRef();
            var manifest = new ASiCManifestType();
            manifest.DataObjectReference = entries.Select(ToDataObject).ToArray();
            manifest.SigReference = new SigReferenceType
            {
                MimeType = signatureFileRef.MimeType.ToString(), URI = signatureFileRef.FileName
            };
            using (var outStream = new MemoryStream())
            using (var xmlWriter = CreateXmlWriter(outStream))
            {
                new XmlSerializer(typeof(ASiCManifestType)).Serialize(xmlWriter, manifest, CreateNamespaces());
                return new ManifestContainer(FILENAME, outStream.ToArray(), signatureFileRef, ManifestSpec.Cades);
            }
        }

        private static XmlWriter CreateXmlWriter(Stream outStream)
        {
            var writer = XmlWriter.Create(outStream, CreateXmlWriterSettings());
            writer.WriteRaw($"<?xml version=\"1.0\" encoding=\"{Encoding.HeaderName.ToUpper(CultureInfo.CurrentCulture)}\" standalone=\"yes\"?>\n");
            return writer;
        }

        private static XmlWriterSettings CreateXmlWriterSettings()
        {
            return new XmlWriterSettings
            {
                Encoding = Encoding,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Document,
                Indent = true
            };
        }

        private static XmlSerializerNamespaces CreateNamespaces()
        {
            var ns = new XmlSerializerNamespaces();
            ns.Add("cades", Namespaces.CadesAsicNamespace);
            ns.Add("sig", Namespaces.XmlSignatureNamespace);
            return ns;
        }

        private static DataObjectReferenceType ToDataObject(AsicePackageEntry packageEntry)
        {
            if (packageEntry == null)
            {
                return null;
            }

            return new DataObjectReferenceType
            {
                MimeType = packageEntry.Type.ToString(),
                DigestMethod = new DigestMethodType
                {
                    Algorithm = packageEntry.MessageDigestAlgorithm.Uri.ToString()
                },
                DigestValue = packageEntry.Digest.GetDigest(),
                URI = packageEntry.FileName
            };
        }
    }
}