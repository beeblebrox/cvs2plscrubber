using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlManager;
using System.IO;

namespace Scrubber
{
    class Program
    {
        private static string fromFile_;
        private static string toFile_;

        static void Main(string[] args)
        {
#if DEBUG
            fromFile_ = @"C:\gource\gdata\cvs2pl.xml";
            toFile_ = fromFile_ + @".scrubbed";
#else
            //TODO:  Command line options
#endif
            XmlNode doc = XmlManager.XmlProcessor.ReadFromXml<XmlNode>(fromFile_, new XmlScrubHandler());
            FileStream fs = File.Create(toFile_, 1000);
            StreamWriter sw = new StreamWriter(fs);
            string XML = XmlProcessor.XmlDeclaration + XmlProcessor.ToXml(doc,false);
            sw.Write(XML);
            sw.Close();
            
        }
    }
}
