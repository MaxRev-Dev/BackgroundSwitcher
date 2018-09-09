using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static PexelsClient;

namespace BackgroundSwitcher
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Console.Title = "BackgroundSwitcher";
            Worker worker;
            do
            {
                if (args != null && args.Count() > 0)
                {
                    worker = ProcessArgs(args);
                }
                else
                {
                    Greeting();

                    do
                    {
                        var oargs = Console.ReadLine();
                        args = oargs.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < args.Length; i++)
                        {
                            if (args[i].Contains('"'))
                            {
                                var str = new Regex("\"(.*)\"").Match(oargs).Groups[1].Value;
                                args = args.Take(i).Concat(new[] { str }).Concat(args.Skip(i + 1 + str.Count(x => x == ' '))).ToArray();
                            }
                        }
                        worker = ProcessArgs(args);
                        if (worker == null)
                            Console.WriteLine("Incorrect input. Please, try again");
                        else break;

                    } while (true);
                }
                try
                {
                    Task.WaitAll(worker.DoWork());
                }
                catch (Exception ex) when (ex.InnerException.Message.Contains("403"))
                {
                    Apikey = null;
                    Console.WriteLine("Invalid api key provided. Please, contact with Pexels support team");
                }
            } while (true);
        }

        private static string Apikey = null;
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Wallpaper.Dispose();
        }

        private static Worker ProcessArgs(string[] args)
        {
            Worker w = new Worker();
            try
            {
                var intOk = int.TryParse(args[1], out var ms);
                w.TimeoutMS = intOk ? ms : 5000;
                int next = intOk ? 2 : 1;
                w.UseTmpPath = args.Contains("useTmp");
                switch (args[0])
                {
                    case "localShow":
                        {
                            w.Path = args[next];
                            if (!Directory.Exists(w.Path)) { Console.WriteLine("Is directory exists?"); return null; }
                            break;
                        }
                    case "pexelsShow":
                        {
                            if (!IsConnectionOk())
                            {
                                Console.WriteLine("ERROR: No Internet connection available");
                                Task.Delay(5000).Wait();
                                Environment.Exit(-1);
                            }
                            w.Keyword = args[next].Trim('"');
                            KeyPrompt(w);
                            break;
                        }
                    default:
                        return default;
                }
            }
            catch (IndexOutOfRangeException) { return null; }

            return w;

        }
        private static void KeyPrompt(Worker worker)
        {
            if (Apikey != null) worker.SetKey(Apikey);
            else
            {
                Console.WriteLine("Please, provide your Pexels api key");
                worker.SetKey(Apikey = Console.ReadLine());
            }
        }
        private static bool IsConnectionOk()
        {
            return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }

        private class Worker
        {
            public void SetKey(string apiKey)
            {
                Client = new PexelsClient(apiKey);
            }
            private PexelsClient Client { get; set; }
            public string Keyword { get; set; }
            public int TimeoutMS { get; set; }
            public bool UseTmpPath { get; set; }
            public string Path { get; set; }
            private int pageID = 1;

            private Page CurrentPage { get; set; }
            public async Task ProcessRequest(int pageID)
            {
                if (Keyword != null)
                {
                    Console.WriteLine($"Using keyword: {Keyword}");
                    CurrentPage = await Client.SearchAsync(Keyword, pageID, 40);
                }
                else
                {
                    Console.WriteLine($"Using popular wall");
                    CurrentPage = await Client.PopularAsync(pageID, 40);
                }
                CurrentTaskList = CurrentPage.Photos;
                Console.WriteLine($"Current / Total: {CurrentPage.PerPage}/{CurrentPage.TotalResults}");
            }
            public async Task DoWork()
            {
                do
                {
                    if (Path != null)
                        LoadAllFromFolder();
                    else
                    {
                        Console.WriteLine($"Page {pageID}");
                        await ProcessRequest(pageID++);
                    }
                    await SetImages();
                } while (CurrentPage?.NextPage != null);
            }

            private void LoadAllFromFolder()
            {
                CurrentTaskList =
                    Directory.GetFiles(Path, "*.jpg", SearchOption.AllDirectories)
                    .Select(x => new Photo() { Local = x }).ToList();
            }

            private async Task SetImages()
            {
                foreach (var i in CurrentTaskList)
                {
                    Wallpaper.Set(i, Wallpaper.Style.Fill, Keyword, UseTmpPath);
                    if (i.Local == null)
                    {
                        Console.WriteLine(
                            $@"
Photo ID: {i.Id} Author: {i.Photographer}
{i.Height}x{i.Width} URL: {i.Url} 
");
                    }
                    else
                        Console.WriteLine($"Now local: {i.Local}");
                    await Task.Delay(TimeoutMS);
                }
            }

            private List<Photo> CurrentTaskList { get; set; } = new List<Photo>();

        }

        private static void Greeting()
        {
            Console.WriteLine($@"Hi. This app was created by MaxRev
BackgroundSwitcher will run slideshow from folder or load images from pexels.com

>>> For show SlideShow from folder type
 localShow [showTimeoutInMilliseconds(default:5000)] [pathToImages]
 Ex: localShow 5000 {Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)} 
 Ex: localShow {Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)} 

>>> For images download and slideshow
 pexelsShow [showTimeoutInMilliseconds(default:5000)] ""keywords like on pexels search"" [useTmp(optional)]
 Ex: pexelsShow 5000 ""mountains 4k""
 Ex: pexelsShow ""trees -car""
 Ex: pexelsShow ""car"" useTmp
NOTE: useTmp is flag to use temporary folder to download files


Have fun and enjoy pics!!!

");
        }
    }
}
