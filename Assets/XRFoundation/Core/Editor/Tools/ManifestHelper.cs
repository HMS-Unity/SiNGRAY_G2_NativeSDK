using System.Xml.Linq;

public class ManifestHelper
{
    private XDocument doc;
    private XNamespace ns = @"http://schemas.android.com/apk/res/android";

    public ManifestHelper(string path)
    {
        doc = XDocument.Load(path);
    }

    public void Save(string path)
    {
        doc.Save(path);
    }

    public void SetVersions(string versionName, int versionCode)
    {
        doc.Root.SetAttributeValue(ns + "versionCode", versionCode);
        doc.Root.SetAttributeValue(ns + "versionName", versionName);
    }
}