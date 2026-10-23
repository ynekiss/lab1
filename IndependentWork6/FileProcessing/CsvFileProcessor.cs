namespace IndependentWork6.FileProcessing
{
    public class CsvFileProcessor : FileProcessor
    {
        public override string FormatName => "CSV";

        protected override string ProcessContent(string content)
        {
            string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim()).ToArray();
            string[] header = lines[0].Split(',');
            // Сума по останньому стовпцю (оцінка)
            double average = lines.Skip(1)
                                  .Select(l => double.Parse(l.Split(',')[^1], System.Globalization.CultureInfo.InvariantCulture))
                                  .Average();
            return $"стовпців: {header.Length}, записів: {lines.Length - 1}, середня оцінка: {average:F2}";
        }
    }
}
