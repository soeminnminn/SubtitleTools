using System.Text.Json;
using System.Text.Json.Serialization;

namespace SubtitleEdit.Plugins.Cleaner
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // A Subtitle Edit 5 plugin is just an executable:
            //   1. read the request file (its path is the first command-line argument),
            //   2. transform the subtitle,
            //   3. write the response file (path is given in the request),
            //   4. exit with code 0.

            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: Cleaner <requestFilePath>");
                return 1;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
            };

            PluginRequest request = null;
            try
            {
                request = JsonSerializer.Deserialize<PluginRequest>(File.ReadAllText(args[0]), jsonOptions);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Could not read request: " + exception.Message);
                return 1;
            }

            if (request is null || string.IsNullOrEmpty(request.ResponseFilePath))
            {
                Console.Error.WriteLine("Invalid request.");
                return 1;
            }

            // Work on the SubRip representation - it is always provided in the request.
            var srt = request.Subtitle.SubRip;
            if (string.IsNullOrEmpty(srt))
            {
                Console.Error.WriteLine("No subtitle loaded");
                return 1;
            }

            string result = string.Empty;

            SubtitleTools.ISubtitle subtitle = new SubtitleTools.Subtitle();
            if (subtitle.Parse(srt))
            {
                SubtitleTools.Cleaner cleaner = new SubtitleTools.Cleaner();
                cleaner.Clean(ref subtitle);
                result = subtitle.ToString();
            }

            var response = new PluginResponse
            {
                Status = "ok",
                Message = "Subtitle cleaned.",
                UndoDescription = "Cleaner 1.0.0",
                Subtitle = new PluginSubtitle
                {
                    Format = "SubRip",
                    Native = result,
                },
            };

            File.WriteAllText(request.ResponseFilePath, JsonSerializer.Serialize(response, jsonOptions));

            return 0;
        }
    }
}
