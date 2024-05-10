using System;
using System.Net;
using System.Text;
using System.IO.Compression;
using System.Threading;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;

namespace Proj1
{
	class Program
	{
        private static readonly object obj = new object();
        private static string root = "C:\\Users\\Dunja\\source\\repos\\Proj1\\Proj1\\Fajlovi";

        private static Dictionary<string, byte[]> cache = new Dictionary<string, byte[]>();

        static void Main()
        {
            HttpListener hListener = new HttpListener();
            hListener.Prefixes.Add("http://127.0.0.1:5000/");
            hListener.Start();
            Console.WriteLine("Server spreman: (http://127.0.0.1:5000/)");
            while (true)
            {
                var context = hListener.GetContext();
                ThreadPool.QueueUserWorkItem(state => { Server(context); });
            }

        }


        static void SendResponse(HttpListenerContext context, byte[] body, string type = "text/plain; charset=utf-8", HttpStatusCode status = HttpStatusCode.OK)
        {
            HttpListenerResponse response = context.Response;
            response.ContentType = type;
            response.ContentLength64 = body.Length;
            response.StatusCode = (int)status;
            if (type == "application/zip")
            {
                response.AddHeader("Content-Disposition", $"attachment; filename=\"ZipFajlovi.zip\"");
            }
            System.IO.Stream output = response.OutputStream;
            output.Write(body, 0, body.Length);
            output.Close();
        }
        static void Server(object c)
        {
            HttpListenerContext context = (HttpListenerContext)c;
            try
            {
                if (context.Request.HttpMethod != HttpMethod.Get.Method)
                {
                    SendResponse(context, Encoding.UTF8.GetBytes("\nDozvoljen je samo GET"), "text/plain; charset=utf-8", HttpStatusCode.BadRequest);
                    return;
                }

                var fileNames = context.Request.Url.PathAndQuery.TrimStart('/').Split('&');
                fileNames = fileNames.Where(file => file != string.Empty).ToArray();

                if (fileNames.Length == 0)
                {
                    SendResponse(context, Encoding.UTF8.GetBytes("\nNije naveden nijedan fajl"), "text/plain; charset=utf-8", HttpStatusCode.BadRequest);
                    return;
                }


                fileNames = fileNames.Where(file => File.Exists(Path.Combine(root, file))).ToArray();
                if (fileNames.Length == 0)
                {
                    SendResponse(context, Encoding.UTF8.GetBytes("\nNe postoji nijedan od navedenih fajlova na serveru"), "text/plain; charset=utf-8", HttpStatusCode.NotFound);
                    return;
                }
                SendResponse(context, Zipuj(fileNames), "application/zip");


            }
            catch (System.Exception e)
            {
                SendResponse(context, Encoding.UTF8.GetBytes(e.Message), "text/plain; charset=utf-8", HttpStatusCode.InternalServerError);
            }


        }


        static byte[] Zipuj(string[] fileNames)
        {
            Array.Sort(fileNames, (x, y) => String.Compare(x, y));
            string filenameh = String.Join(',', fileNames);
            try
            {
                if (root == "") root = Environment.CurrentDirectory;

                lock (obj)
                {
                    byte[] res;
                    if (cache.TryGetValue(filenameh, out res))
                    {
                        Console.WriteLine($"\nPronadjen u cache-u: {filenameh}");
                        return res;
                    }
                }
                byte[] zipbytes;
                using (MemoryStream mem = new MemoryStream())
                {
                    using (ZipArchive zip = new ZipArchive(mem, ZipArchiveMode.Create))
                    {
                        foreach (string f in fileNames)
                        {
                            zip.CreateEntryFromFile(Path.Combine(root, f), f, CompressionLevel.Optimal);
                        }
                    }
                    zipbytes = mem.GetBuffer();
                }
                lock (obj)
                {
                    Console.WriteLine($"\nDodat u cache: {filenameh}");
                    cache[filenameh] = zipbytes;
                }

                return zipbytes;

            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }
}
