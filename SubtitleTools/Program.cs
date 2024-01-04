using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SubtitleTools
{
    class Program
    {
        static void Main(string[] args)
        {
            var BOM = new byte[] { 0xef, 0xbb, 0xbf };
            string filePath = string.Empty;
#if DEBUG
            filePath = @"D:\Projects\SubtitleEdit\example\IndependenceDay.srt";
#endif
            if (args.Length > 0)
            {
                filePath = args[0];
            }

            if (File.Exists(filePath))
            {
                try
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
                        stream.Write(BOM, 0, BOM.Length);

                        using (var writer = new StreamWriter(stream, Encoding.UTF8))
                        {
                            writer.Write(subTitle.ToString());
                            writer.Flush();
                        }
                    }

                    Console.WriteLine("OUT: " + output);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: " + e.Message);
#if DEBUG
                    Console.WriteLine(e.StackTrace);
#endif
                }
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

