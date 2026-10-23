namespace IndependentWork6.FileProcessing
{
    public class TextFileProcessor : FileProcessor
    {
        public override string FormatName => "TXT";

        protected override string ProcessContent(string content)
        {
            string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return $"рядків: {lines.Length}, слів: {words}";
        }
    }
}
