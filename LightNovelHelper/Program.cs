using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using System;
using System.Text;

namespace LightNovelHelper
{
    internal class Program
    {
        static List<FileInfo> Files { get; set; } = new ();

        async static Task Main(string[] args)
        {
            var path = Directory.GetCurrentDirectory();
            var di = new DirectoryInfo(path);
            var taskList = new List<Task>();

            foreach (var dri in di.GetDirectories())
            {
                RootNovel(dri);
            }

            foreach (var fi in Files.Where(x => x.Extension == ".html").ToList())
            {
                taskList.Add(Task.Run(async () => await ReadHtml(fi)));
            }

            foreach (var garbage in Files.Where(x => x.Name == "code.min1ab2.js").ToList())
            {
                garbage.Delete();
            }

            await Task.WhenAll(taskList.ToArray());

            Console.WriteLine("Done");
        }

        async static Task ReadHtml(FileInfo fi)
        {
            var str = await File.ReadAllTextAsync(fi.FullName);

            var big5 = ChineseConverter.Convert(str, ChineseConversionDirection.SimplifiedToTraditional);

            var result = big5.Replace("'/novel", "'../../novel")
                             .Replace("嗶哩輕小說", "");

            await File.WriteAllTextAsync(fi.FullName, result);
        }

        static void RootNovel(DirectoryInfo dir)
        {
            FileSearch(dir.FullName);
        }


        static void FileSearch(string sDir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        Files.Add(new FileInfo(f));
                    }

                    FileSearch(d);
                }
            }
            catch (Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }
    }
}
