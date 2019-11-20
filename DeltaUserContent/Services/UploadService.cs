using LibDeltaSystem.Db.System;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;
using DeltaUserContent.FileAdapters;

namespace DeltaUserContent.Services
{
    public static class UploadService
    {
        public const int BUFFER_SIZE = 2048;

        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Authenticate the user
            DbUser user = await Program.conn.AuthenticateUserToken(e.Request.Headers["Authorization"].ToString().Substring("Bearer ".Length));
            if(user == null)
            {
                e.Response.StatusCode = 401;
                await Program.WriteStringToStreamAsync(e.Response.Body, "Not Authenticated");
                return;
            }

            //Make sure that the content length is within range
            if(!e.Request.ContentLength.HasValue)
            {
                e.Response.StatusCode = 411;
                await Program.WriteStringToStreamAsync(e.Response.Body, "Content-Type Header is Required");
                return;
            }
            if(e.Request.ContentLength.Value > Program.config.max_filesize)
            {
                e.Response.StatusCode = 413;
                await Program.WriteStringToStreamAsync(e.Response.Body, "File Too Large");
                return;
            }

            //Get the application
            BaseAdapter adapter;
            string application_id = e.Request.Query["application_id"];
            if (Program.applications.ContainsKey(application_id))
                adapter = Program.applications[application_id];
            else
            {
                e.Response.StatusCode = 400;
                await Program.WriteStringToStreamAsync(e.Response.Body, "Invalid Application ID");
                return;
            }

            //Generate a filename to use
            string name = LibDeltaSystem.Tools.SecureStringTool.GenerateSecureString(24);
            while(File.Exists(Program.config.content_path+name))
                name = LibDeltaSystem.Tools.SecureStringTool.GenerateSecureString(24);

            //Create a random token to use
            string token = LibDeltaSystem.Tools.SecureStringTool.GenerateSecureString(48);
            while (await Program.conn.GetUserContentByToken(token) != null)
                token = LibDeltaSystem.Tools.SecureStringTool.GenerateSecureString(48);

            //Create a DB entry
            DbUserContent uc = new DbUserContent
            {
                node_name = Program.config.node_name,
                size = (int)e.Request.ContentLength.Value,
                token = token,
                type = e.Request.ContentType,
                uploader = user._id,
                upload_time = DateTime.UtcNow,
                url = Program.config.content_url + name,
                name = name,
                _id = ObjectId.GenerateNewId(),
                application_id = application_id
            };

            //Create a file to write to
            using (FileStream fs = new FileStream(Program.config.content_path + name, FileMode.Create))
            using (MemoryStream ms = new MemoryStream())
            {
                //Copy to stream
                byte[] buffer = new byte[BUFFER_SIZE];
                int remaining = (int)e.Request.ContentLength.Value;
                while (remaining > 0)
                {
                    int read = await e.Request.Body.ReadAsync(buffer, 0, BUFFER_SIZE);
                    await ms.WriteAsync(buffer, 0, read);
                    remaining -= read;
                }

                //Process
                ms.Position = 0;
                await adapter.ProcessData(uc, ms, fs);
            }

            //Insert
            await Program.conn.system_user_uploads.InsertOneAsync(uc);

            //Write data
            e.Response.ContentType = "application/json";
            await Program.WriteStringToStreamAsync(e.Response.Body, JsonConvert.SerializeObject(uc, Formatting.Indented));
        }
    }
}
