using LibDeltaSystem.Db.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaUserContent.FileAdapters
{
    public abstract class BaseAdapter
    {
        /// <summary>
        /// Processes the data.
        /// </summary>
        /// <param name="uc">File info</param>
        /// <param name="input">Input stream</param>
        /// <param name="output">Output stream</param>
        /// <returns>Not null if there was an error</returns>
        public abstract Task<string> ProcessData(DbUserContent uc, Stream input, Stream output);
        public abstract void SetConfig(ServiceConfig_Application app);
    }
}
