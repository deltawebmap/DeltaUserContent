using LibDeltaSystem.Db.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaUserContent.Services
{
    public static class FileService
    {
        public static async Task OnHttpRequest(Microsoft.AspNetCore.Http.HttpContext e)
        {
            //Get the file data
            DbUserContent uc = await Program.conn.GetUserContentByName(e.Request.Path.ToString().Split('/')[2]);
            if(uc == null)
            {
                e.Response.StatusCode = 404;
                await Program.WriteStringToStreamAsync(e.Response.Body, "Not Found");
                return;
            }

            //Set headers
            e.Response.ContentLength = uc.size;
            e.Response.ContentType = uc.type;
            e.Response.Headers.Add("X-Uploader-ID", uc.uploader.ToString());
            e.Response.Headers.Add("X-Uploader-Node", uc.node_name);
            e.Response.Headers.Add("Date", uc.upload_time.ToUniversalTime().ToString("r"));

            //If we requested a download, set the content type
            if (e.Request.Query["download"] == "true")
                e.Response.ContentType = "application/octet-strean";

            //Open file stream and copy
            using (FileStream fs = new FileStream(Program.config.content_path + uc.name, FileMode.Open, FileAccess.Read))
                await fs.CopyToAsync(e.Response.Body);
        }
    }
}
