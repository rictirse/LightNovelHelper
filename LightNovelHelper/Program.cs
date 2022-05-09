using HtmlAgilityPack;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using System;
using System.Reflection;
using System.Text;

namespace LightNovelHelper
{
    internal class Program
    {
        static List<FileInfo> Files { get; set; } = new ();
        /// <summary>
        /// 劇情相關詞彙
        /// </summary>
        static IEnumerable<string[]> PlotMappingList { get; set; } 
        /// <summary>
        /// 慣用詞彙
        /// </summary>
        static IEnumerable<string[]> HabitualMappingList { get; set; } 
        /// <summary>
        /// 垃圾檔案清單
        /// </summary>
        static IEnumerable<string> GarbageList { get; set; }
        /// <summary>
        /// 垃圾資料夾
        /// </summary>
        static IEnumerable<string> GarbageDirList = new[]
        {
            "hts-cache",
            "pagead2.googlesyndication.com",
            "pagead2.googlesyndication.com",
            "hm.baidu.com",
        };

        async static Task Main(string[] args)
        {
            Console.WriteLine($"{DateTime.Now} Start");
            var taskList = new List<Task>();
            
            taskList.Add(Task.Run(async () => await ReadGarbageList()));
            taskList.Add(Task.Run(async () => await ReadMappingList()));
            taskList.Add(Task.Run(() => LoadFiles()));

            await Task.WhenAll(taskList.ToArray());
            taskList.Clear();
            Console.WriteLine($"{DateTime.Now} Init done.");

            var fiList = Files.Where(x => x.Extension == ".html").ToList();
            foreach (var fi in fiList)
            {
                taskList.Add(Task.Run(async () => await ReadHtml(fi)));
            }

            foreach (var readToolsJs in Files.Where(x => x.Name == "readtoolsffc1.js").ToList())
            {
                File.WriteAllBytes(readToolsJs.FullName, "readtoolsffc1.js".ResourceToByteArray());
            }

            foreach (var garbage in Files.Where(x => GarbageList.Contains(x.Name)).ToList())
            {
                garbage.Delete();
            }

            await Task.WhenAll(taskList.ToArray());

            Console.WriteLine($"{DateTime.Now} Covered done.");
        }

        /// <summary>
        /// 讀詞彙替代
        /// </summary>
        /// <returns></returns>
        async static Task ReadMappingList()
        {
            PlotMappingList = (await "PlotMappingList.csv".ReadFile()).ReadCsvFile();
            HabitualMappingList = (await "HabitualMapping.csv".ReadFile()).ReadCsvFile();

            //var g = HabitualMappingList.GroupBy(x => $"{x[0]}").Select(x=>x.First()).ToList();
            //var s = string.Join("\r\n", g.Select(x => $"{x[0]},{x[1]}"));
        }

        /// <summary>
        /// 讀垃圾清單
        /// </summary>
        /// <returns></returns>
        async static Task ReadGarbageList() =>
            GarbageList = await "Garbage.txt".ReadFile();
        
        async static Task ReadHtml(FileInfo fi)
        {
            //效能不好建議只執行第一次
            HtmlProcedure(fi.FullName);
            var str = await File.ReadAllTextAsync(fi.FullName);

            var big5 = ChineseConverter.Convert(str, ChineseConversionDirection.SimplifiedToTraditional);
            var result = big5;

            foreach (var mapping in HabitualMappingList)
            {
                if (mapping.Count() != 2) continue;
                result = result.Replace(mapping[0], mapping[1]);
            }

            foreach (var mapping in PlotMappingList)
            {
                if (mapping.Count() != 2) continue;
                result = result.Replace(mapping[0], mapping[1]);
            }

            await File.WriteAllTextAsync(fi.FullName, result);
        }

        #region Html
        static void HtmlProcedure(string htmlPath)
        {
            var doc = new HtmlDocument();
            doc.Load(htmlPath, Encoding.UTF8);

            if (htmlPath.EndsWith("catalog.html"))
            {
                RemoveCatalog();
            }
            else 
            {
                RemoveReadPage();
            }

            void RemoveCatalog()
            {
                try
                {
                    doc.RemoveSingleNode("//footer");
                    doc.RemoveSingleNode("//div[@class='search-popup']");
                    doc.RemoveSingleNode("//div[@class='header-operate']");

                    var script = doc.DocumentNode.SelectNodes("//script");

                    if (script != null)
                    { 
                        var findScript = script.Where(x => x.OuterHtml.Contains("commondf33.js"))
                            .Union(script.Where(x => x.OuterHtml.Contains("GB_BIG51534.js")))
                            .Union(script.Where(x => x.OuterHtml.Contains("windows phone")))
                            .Union(script.Where(x => x.OuterHtml.Contains("linovelib.com")))
                            .Union(script.Where(x => x.OuterHtml.Contains("baidu"))).ToList();

                        foreach (var s in findScript)
                        {
                            s.Remove();
                        }
                    }

                    doc.Save(htmlPath);
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex);
                }
            }

            void RemoveReadPage()
            { 
                try
                {
                    var script = doc.DocumentNode.SelectNodes("//script");
                    if (script != null)
                    {
                        var findScript = script.Where(x => x.OuterHtml.Contains("hma361.js"))
                            .Union(script.Where(x => x.OuterHtml.Contains("baidu"))).ToList();

                        foreach (var s in findScript)
                        {
                            s.Remove();
                        }
                    }

                    var footlinkNode = doc.DocumentNode.SelectSingleNode("//div[@class='footlink']");
                    if (footlinkNode != null)
                    {
                        var btn = footlinkNode.SelectNodes("//a");
                        if (btn.Count() == 5)
                        {
                            btn[2].Remove();
                            btn[4].Remove();
                        }
                    }

                    doc.Save(htmlPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        #endregion

        /// <summary>
        /// 讀檔案
        /// </summary>
        static void LoadFiles()
        {
            var path = Directory.GetCurrentDirectory();
            var di = new DirectoryInfo(path);
            foreach (var dri in di.GetDirectories())
            {
                FileSearch(dri.FullName);
            }
        }

        /// <summary>
        /// 資料夾搜尋，並將搜尋到的檔案加到檔案清單
        /// </summary>
        static void FileSearch(string sDir)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    try
                    {
                        var di = new DirectoryInfo(d);
                        if (GarbageDirList.Contains(di.Name))
                        {
                            foreach (var file in di.EnumerateFiles())
                            {
                                file.Delete();
                            }
                            foreach (var subDirectory in di.EnumerateDirectories())
                            {
                                subDirectory.Delete(true);
                            }
                            di.Delete(true);
                            continue;
                        }
                    }
                    catch { }

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

    static class NovelExtensions
    {
        /// <summary>
        /// Embedded resource轉byte
        /// </summary>
        public static byte[] ResourceToByteArray(this string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                                       .Where(str => str.Contains(fileName))
                                       .FirstOrDefault();
            if (string.IsNullOrEmpty(resourceName)) return null;

            using (var s = assembly.GetManifestResourceStream(resourceName))
            using (var ms = new MemoryStream())
            {
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 讀Csv格式
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static IEnumerable<string[]> ReadCsvFile(this IEnumerable<string> @this) =>
            @this.Select(x => x.Split(",")).Where(x => x.Count() == 2).ToArray();

        /// <summary>
        /// 讀檔案
        /// </summary>
        public static async Task<IEnumerable<string>> ReadFile(this string @this) =>
            File.Exists(@this) ?
                await File.ReadAllLinesAsync(@this) :
                new List<string>();

        public static void RemoveNodes(this HtmlDocument doc, string xpath)
        {
            var founds = doc.DocumentNode.SelectNodes(xpath);
            foreach (var f in founds)
            {
                f.Remove();
            }
        }

        public static void RemoveSingleNode(this HtmlDocument doc, string xpath)
        {
            var found = doc.DocumentNode.SelectSingleNode(xpath);
            if (found != null)
            {
                found.Remove();
            }
        }
    }
}
