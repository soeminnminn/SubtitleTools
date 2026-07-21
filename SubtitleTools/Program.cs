using CommandLine;
using CommandLine.Arguments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;


namespace SubtitleTools
{
    class Program
    {
        private static CommandLineParser commandLine = null;

        private static CommandLineParser InitializeCommandLineParser()
        {
            if (commandLine == null)
            {
                commandLine = new CommandLineParser()
                {
                    AcceptDefault = true,
                    AcceptHyphen = true,
                    ShowUsageOnEmptyCommandline = true,
                    ShowUsageHeader = "SubtitleTools inputSubtitleFile [-o outputFileName] [-s] [-h] [-e] [-t] [-m] [-d]" + "\n\n" +
                        "Supported input subtitle file formats:" + "\n" +
                        "\tMicroDVD format: .sub" + "\n" +
                        "\tSubviewer format: .sub" + "\n" +
                        "\tSubrip format: .srt" + "\n" +
                        "\tSub Station Alpha v4.00+ format: .ass, .ssa" + "\n" +
                        "\tWebVTT format: .vtt" + "\n" +
                        "\tSynchronized Accessible Media Interchange format: .smi" + "\n" +
                        "\tTimed Text Markup Language format: .xml, .ttml" + "\n" +
                        "\tYoutube XML format: .xml" + "\n"
                };

                commandLine.Arguments.Add(new FileArgument('o', "output", "Output file name to save.")
                {
                    FileMustExist = false
                });

                commandLine.Arguments.Add(new SwitchArgument('s', "styles", "Simply styles.", true));
                commandLine.Arguments.Add(new SwitchArgument('h', "hearing", "Remove hearing text.", true));
                commandLine.Arguments.Add(new SwitchArgument('e', "empty", "Remove empty lines.", true));
                commandLine.Arguments.Add(new SwitchArgument('t', "typo", "Typo fix.", true));
                commandLine.Arguments.Add(new SwitchArgument('m', "merge", "Merge lines.", true));
                commandLine.Arguments.Add(new SwitchArgument('d', "dialog", "Remove empty dialogues.", true));
            }
            return commandLine;
        }

        static void Main(string[] args)
        {
#if TEST
            Test();
#else
            CommandLineParser commandLineParser = InitializeCommandLineParser();
            
            try
            {
                commandLineParser.ParseCommandLine(args);

                if (commandLineParser.ParsingSucceeded)
                {
                    var target = new ParsingTarget();
                    commandLineParser.ExtractArgumentAttributes(target);

                    string filePath = commandLineParser.DefaultArgument;
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        ISubtitle subTitle = Subtitle.FromFile(filePath);
                        Console.WriteLine("Count >> " + ((ICollection<Dialogue>)subTitle).Count);

                        Cleaner cleaner = new Cleaner();
                        cleaner.Clean(ref subTitle);

                        string dir = Path.GetDirectoryName(filePath);
                        string fileName = Path.GetFileNameWithoutExtension(filePath);

                        string output = Path.Combine(dir, fileName + "-out.srt");
#if DEBUG
                        output = "out.srt";
#endif
                        if (File.Exists(output)) File.Delete(output);

                        using (var stream = File.Create(output))
                        {
                            stream.Write(ToolsConstants.UTF8_BOM, 0, ToolsConstants.UTF8_BOM.Length);

                            using (var writer = new StreamWriter(stream, Encoding.UTF8))
                            {
                                writer.Write(subTitle.ToString());
                                writer.Flush();
                            }
                        }

                        Console.WriteLine("OUT: " + output);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.Message);
#if DEBUG
                Console.WriteLine(e.StackTrace);
#endif
            }
#endif

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

#if TEST
        static void Test()
        {
            //StringBuilder builder = new StringBuilder();

            //for (int i = 0; i < text.Length; i++)
            //{
            //    var ch = (int)text[i];
            //    if (ch < 255)
            //    {
            //        builder.Append(text[i]);
            //    }
            //    else
            //    {
            //        builder.AppendFormat("\\u{0:X4}", ch);
            //    }                    
            //}
            //Console.WriteLine(builder.ToString());

            string input = "The quick \"brown fox\"` jump \"over lazy` dog.";

            var sl = new StringLiteralMatcher(input);

            while(sl.MoveNext())
            {
                Console.WriteLine($"[{sl.Current}]");
            }
        }
#endif

        class ParsingTarget
        {
            [FileArgument('i', "input")]
            public FileInfo InputFile { get; set; }

            [FileArgument('o', "output")]
            public FileInfo OutputFile { get; set; }

            [SwitchArgument('s', "styles", true)]
            public bool SimplyStyles { get; set; }

            [SwitchArgument('h', "hearing", true)]
            public bool RemoveHearingText { get; set; }

            [SwitchArgument('e', "empty", true)]
            public bool RemoveEmptyLine { get; set; }

            [SwitchArgument('t', "typo", true)]
            public bool TypoFix { get; set; }

            [SwitchArgument('m', "merge", true)]
            public bool MergeLines { get; set; }

            [SwitchArgument('d', "dialog", true)]
            public bool RemoveEmptyDialogues { get; set; }
        }
    }
}

