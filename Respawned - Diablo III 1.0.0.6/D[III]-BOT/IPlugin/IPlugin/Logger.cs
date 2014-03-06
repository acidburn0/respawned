using System;
using System.IO;

namespace IPlugin
{
    sealed public class Logger : IDisposable
    {
        private StreamWriter _writer;
        private static Logger _instance;

        private Logger(string pFileName)
        {
            try
            {
                _writer = new StreamWriter(pFileName, true);
            }
            catch { }
        }

        public static Logger GetInstance()
        {
            if (_instance == null)
            {
               _instance = new Logger("log.rsp");
            }
            return _instance;
        }

        public void Log(string pText)
        {
            try
            {
                Console.WriteLine("[" + DateTime.Now.ToString() + "] " + pText);

                _writer.WriteLine("[" + DateTime.Now + "] " + pText);
                _writer.Flush();
            }
            catch { }
        }

        public void LogDebug(string pText)
        {
            Console.WriteLine("[" + DateTime.Now.ToString() + "] " + pText);
        }

        public void Dispose()
        {
            try
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
            catch { }
        }
    }
}
