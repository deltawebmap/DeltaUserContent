using DeltaUserContent.FileAdapters;
using LibDeltaSystem;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaUserContent
{
    class Program
    {
        public static ServiceConfig config;
        public static DeltaConnection conn;
        public static Dictionary<string, BaseAdapter> applications;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            //Get config
            config = Newtonsoft.Json.JsonConvert.DeserializeObject<ServiceConfig>(File.ReadAllText(args[0]));

            //Create adapters
            applications = new Dictionary<string, BaseAdapter>();
            foreach(var a in config.applications)
            {
                //Determine type
                BaseAdapter adapter;
                switch(a.type)
                {
                    case "IMAGE":
                        adapter = new FileAdapters.IMAGE.ImageAdapter();
                        break;
                    default:
                        throw new Exception($"Cannot start because there is an invalid application. {a.name} ({a.id}) has an invalid type {a.type}!");
                }

                //Set up
                adapter.SetConfig(a);

                //Add to applications list
                applications.Add(a.id, adapter);
            }
            
            //Connect to database
            conn = new DeltaConnection(config.database_config, "user-content NODE "+config.node_name, 0, 0);
            await conn.Connect();

            //Set up web server
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    IPAddress addr = IPAddress.Any;
                    options.Listen(addr, config.port);

                })
                .UseStartup<Program>()
                .Build();

            await host.RunAsync();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(OnHttpRequest);
        }

        public static async Task WriteStringToStreamAsync(Stream s, string content)
        {
            byte[] data = Encoding.UTF8.GetBytes(content);
            await s.WriteAsync(data);
        }

        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            try
            {
                //Add headers
                e.Response.Headers.Add("Server", "Delta Web Map User Content");
                e.Response.Headers.Add("Access-Control-Allow-Headers", "Authorization, Content-Type, Content-Length");
                e.Response.Headers.Add("Access-Control-Allow-Origin", "https://deltamap.net");
                e.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS, DELETE, PUT, PATCH");

                //If this is options, do cors
                if (e.Request.Method.ToUpper() == "OPTIONS")
                {
                    await WriteStringToStreamAsync(e.Response.Body, "CORS OK");
                    return;
                }

                //Commit
                if (e.Request.Path == "/upload")
                    await Services.UploadService.OnHttpRequest(e);
                else if (e.Request.Path.ToString().StartsWith("/u/"))
                    await Services.FileService.OnHttpRequest(e);
                else
                {
                    e.Response.StatusCode = 404;
                    await WriteStringToStreamAsync(e.Response.Body, "Not Found");
                }
            }
            catch (Exception ex)
            {
                //Log and display error
                var error = await conn.LogHttpError(ex, new System.Collections.Generic.Dictionary<string, string>());
                e.Response.StatusCode = 500;
                await WriteStringToStreamAsync(e.Response.Body, JsonConvert.SerializeObject(error, Newtonsoft.Json.Formatting.Indented));
            }
        }
    }
}
