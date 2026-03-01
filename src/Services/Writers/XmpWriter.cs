using ImageMagick;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace EdsMediaArchiver.Services.Writers;

public interface IXmpWriter
{
    void UpdateImageXmpProfile(MagickImage image, DateTimeOffset date);
}

public class XmpWriter : IXmpWriter
{
    private static readonly XNamespace RdfNs = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private static readonly XNamespace XmpNs = "http://ns.adobe.com/xap/1.0/";
    private static readonly XNamespace ExifNs = "http://ns.adobe.com/exif/1.0/";
    private static readonly XNamespace TiffNs = "http://ns.adobe.com/tiff/1.0/";
    private static readonly XNamespace XNs = "adobe:ns:meta/";

    public void UpdateImageXmpProfile(MagickImage image, DateTimeOffset date)
    {
        var xmpDate = date.ToString("yyyy-MM-ddTHH:mm:ssK");
        var doc = LoadOrCreateXmpDocument(image, xmpDate);

        var description = doc.Descendants(RdfNs + "Description").FirstOrDefault();

        if (description is null)
        {
            doc = CreateBaseXmp(xmpDate);
            description = doc.Descendants(RdfNs + "Description").First();
        }

        SetOrAddProperty(description, XmpNs + "CreateDate", xmpDate);
        SetOrAddProperty(description, XmpNs + "ModifyDate", xmpDate);
        SetOrAddProperty(description, XmpNs + "MetadataDate", xmpDate);
        SetOrAddProperty(description, ExifNs + "DateTimeOriginal", xmpDate);
        SetOrAddProperty(description, ExifNs + "DateTimeDigitized", xmpDate);
        SetOrAddProperty(description, TiffNs + "DateTime", ConvertToTiffDate(xmpDate));

        var xmpBytes = SerializeXmpPacket(doc);
        image.SetProfile(new XmpProfile(xmpBytes));
    }

    private static XDocument LoadOrCreateXmpDocument(MagickImage image, string xmpDate)
    {
        var profile = image.GetXmpProfile();

        if (profile is null)
            return CreateBaseXmp(xmpDate);

        try
        {
            using var ms = new MemoryStream(profile.ToByteArray());
            return XDocument.Load(ms);
        }
        catch
        {
            return CreateBaseXmp(xmpDate);
        }
    }

    private static byte[] SerializeXmpPacket(XDocument doc)
    {
        // Strip any existing xpacket processing instructions to avoid double-wrapping
        doc.Nodes()
            .OfType<XProcessingInstruction>()
            .Where(pi => pi.Target == "xpacket")
            .ToList()
            .ForEach(pi => pi.Remove());

        using var ms = new MemoryStream();
        doc.Save(ms, SaveOptions.DisableFormatting);

        var xmlBytes = ms.ToArray();

        // Strip UTF-8 BOM if present (XDocument.Save writes one)
        if (xmlBytes.Length >= 3 && xmlBytes[0] == 0xEF && xmlBytes[1] == 0xBB && xmlBytes[2] == 0xBF)
            xmlBytes = xmlBytes[3..];

        var begin = "<?xpacket begin=\"\uFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n"u8;
        var end = "\n<?xpacket end=\"w\"?>"u8;

        var result = new byte[begin.Length + xmlBytes.Length + end.Length];
        begin.CopyTo(result);
        xmlBytes.CopyTo(result.AsSpan(begin.Length));
        end.CopyTo(result.AsSpan(begin.Length + xmlBytes.Length));

        return result;
    }

    private static XDocument CreateBaseXmp(string date)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(XNs + "xmpmeta",
                new XAttribute(XNamespace.Xmlns + "x", XNs.NamespaceName),
                new XElement(RdfNs + "RDF",
                    new XAttribute(XNamespace.Xmlns + "rdf", RdfNs.NamespaceName),
                    new XElement(RdfNs + "Description",
                        new XAttribute(RdfNs + "about", ""),
                        new XAttribute(XNamespace.Xmlns + "xmp", XmpNs.NamespaceName),
                        new XAttribute(XNamespace.Xmlns + "exif", ExifNs.NamespaceName),
                        new XAttribute(XNamespace.Xmlns + "tiff", TiffNs.NamespaceName),
                        new XAttribute(XmpNs + "CreateDate", date),
                        new XAttribute(ExifNs + "DateTimeOriginal", date),
                        new XAttribute(ExifNs + "DateTimeDigitized", date),
                        new XAttribute(XmpNs + "ModifyDate", date),
                        new XAttribute(XmpNs + "MetadataDate", date),
                        new XAttribute(TiffNs + "DateTime", ConvertToTiffDate(date))
                    )
                )
            )
        );
    }

    private static string ConvertToTiffDate(string isoDate)
    {
        var dto = DateTimeOffset.Parse(isoDate, null, DateTimeStyles.RoundtripKind);
        return dto.ToString("yyyy:MM:dd HH:mm:ss");
    }

    private static void SetOrAddProperty(XElement parent, XName name, string value)
    {
        var existingAttribute = parent.Attribute(name);
        if (existingAttribute is not null)
        {
            existingAttribute.Value = value;
            return;
        }

        var existingElement = parent.Element(name);
        if (existingElement is not null)
        {
            existingElement.Value = value;
            return;
        }

        parent.Add(new XAttribute(name, value));
    }
}