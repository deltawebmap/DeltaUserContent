using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LibDeltaSystem.Db.System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DeltaUserContent.FileAdapters.IMAGE
{
    public class ImageAdapter : BaseAdapter
    {
        private ImageAdapterSettings settings;

        public override async Task<string> ProcessData(DbUserContent uc, Stream input, Stream output)
        {
            Image<Rgba32> image;

            //Open this as an image
            try
            {
                image = Image<Rgba32>.Load<Rgba32>(input);
            } catch
            {
                return "Error opening the image!";
            }

            //Now, we'll apply transformations
            if(settings.do_resize)
            {
                image.Mutate(x => x.Resize(settings.resize_x, settings.resize_y));
            }

            //Now, we'll write to the output and set values
            image.SaveAsPng(output);
            uc.size = (int)output.Length;
            uc.type = "image/png";

            return null;
        }

        public override void SetConfig(ServiceConfig_Application app)
        {
            settings = app.ReadSettings<ImageAdapterSettings>();
        }

        class ImageAdapterSettings
        {
            public bool do_resize;
            public int resize_x;
            public int resize_y;
        }
    }
}
