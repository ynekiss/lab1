using System.Xml.Linq;

namespace IndependentWork6.FileProcessing
{
    public class XmlFileProcessor : FileProcessor
    {
        public override string FormatName => "XML";

        protected override string ProcessContent(string content)
        {
            XDocument doc = XDocument.Parse(content);
            int elements = doc.Descendants().Count();
            string root = doc.Root?.Name.LocalName ?? "-";
            string names = string.Join(", ", doc.Descendants("student").Select(s => (string)s.Attribute("name")));
            return $"кореневий елемент <{root}>, елементів: {elements}, студенти: {names}";
        }
    }
}
