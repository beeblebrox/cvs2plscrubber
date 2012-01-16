using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlManager;
using System.Text.RegularExpressions;

namespace Scrubber
{
    class XmlScrubHandler : XmlManager.IXmlHandler<XmlManager.XmlNode>
    {
        private XmlManager.XmlNode rootNode;
        private SortedSet<string> unknownTags_;
        private bool processedRoot_ = false;
        private XmlNode wNode_ = null; //Working Node
        private Stack<XmlNode>  parentNodeStack_ = null;
        private XmlNode pNode_ = null; //Current Parrent Node
        private Dictionary<string, string> pathMap_ = new Dictionary<string, string>();
        private HashSet<string> randoms = new HashSet<string>();
        private       Random rnd = new Random();
 
        private static SortedSet<string> SKIPPED_NODES = null;
        private static SortedSet<string> EMPTIED_NODES = null;
        private static SortedSet<string> SCRUBBED_NODES = null;
        private static Regex DIR_PATTERN = new Regex("(?<pathelem>[^/]*)/");
        private static Regex FDIR_PATTERN = new Regex(@"(?<pathelem>.*)/(?<fname>.+?)(?:(?<ext>\.[^.]*$)|$)");
        private static object lock_ = new object();
        public XmlScrubHandler()
        {
            if(SKIPPED_NODES == null)
            lock (lock_)
            {
                if(SKIPPED_NODES == null){
                    SKIPPED_NODES = new SortedSet<string>();
                    SKIPPED_NODES.Add("entry");
                    SKIPPED_NODES.Add("date");
                    SKIPPED_NODES.Add("time");
                    SKIPPED_NODES.Add("isoDate");
                    SKIPPED_NODES.Add("file");
                    SKIPPED_NODES.Add("cvsstate");
                    SKIPPED_NODES.Add("revision");
                    SKIPPED_NODES.Add("weekday");
                    SKIPPED_NODES.Add("tagdate");
                    SKIPPED_NODES.Add("tagisodate");
                    SKIPPED_NODES.Add("tagdatetag");
                }
            }
            if (EMPTIED_NODES == null)
                lock (lock_)
                {
                    if (EMPTIED_NODES == null)
                    {
                        EMPTIED_NODES = new SortedSet<string>();
                        EMPTIED_NODES.Add("msg");
                    }
                }
            if (SCRUBBED_NODES == null)
                lock (lock_)
                {
                    if (SCRUBBED_NODES == null)
                    {
                        SCRUBBED_NODES = new SortedSet<string>();
                        SCRUBBED_NODES.Add("author");
                        SCRUBBED_NODES.Add("name");
                        SCRUBBED_NODES.Add("branch");
                        SCRUBBED_NODES.Add("tag");
                        SCRUBBED_NODES.Add("utag");
                        SCRUBBED_NODES.Add("commondir");
                    }
                }            
            unknownTags_ = new SortedSet<string>();
            parentNodeStack_ = new Stack<XmlNode>();
            //TODO: Add options for forced values
            randoms.Add("documents");
            pathMap_.Add("documents", "bugged");
        }
        public void StartDocument()
        {
        }

        public void EndDocument()
        {
        }

        public void StartElement(string nsURI, string localName, string qName, XmlManager.XmlAttributes attribs)
        {
            wNode_ = new XmlNode(qName, attribs, null);
            if (!processedRoot_)
            {
                if (qName == "changelog")
                {
                    rootNode = wNode_;
                    pNode_ = wNode_;
                    processedRoot_ = true;
                    parentNodeStack_.Push(pNode_);
                    pNode_ = wNode_;
                    return;
                }
                else
                {
                    Console.WriteLine("Root node isn't 'changelog'.");
                    throw new Exception("Root node isn't changelog'.");
                }
            }
            if (SKIPPED_NODES.Contains(qName) || EMPTIED_NODES.Contains(qName) || SCRUBBED_NODES.Contains(qName))
                goto postProcess;

            if (pNode_ == null) //skip parsing.
                return;

            if (!unknownTags_.Contains(qName))
            {
                unknownTags_.Add(qName);
                //TODO option to not write skipped nodes.
                Console.WriteLine("Unknown tag(s) not scrubbed named: " + qName);
            }
            postProcess:
            {
                pNode_.AddChild(wNode_);
                parentNodeStack_.Push(pNode_);
                pNode_ = wNode_;
            }

        }

        public void EndElement(string nsURI, string localName, string qName)
        {
            pNode_ = parentNodeStack_.Pop(); 
            if (parentNodeStack_.Count == 0)
            {
                Console.WriteLine("Finished scrubbing xml document.");
                return;
            }
          }

        public void Text(string text)
        {
            //We always keep the text in the skipped nodes list.
            if (SKIPPED_NODES.Contains(wNode_.Name))
            {
                wNode_.Text = text;
                return;
            }

            //We always empty the text in the emptied nodes list.
            if (EMPTIED_NODES.Contains(wNode_.Name))
            {
                return;
            }
            //The scrubbed nodes needs to be process
            if (SCRUBBED_NODES.Contains(wNode_.Name))
            {
                wNode_.Text = scrubText(text);
                return;
            }
            //TODO, add option to wipe unkown text or ignore it.
            //This is also a failsafe and only prints if the tag was known at the time of
            //writting this but unknow that it could contain text.
            if (!unknownTags_.Contains(wNode_.Name))
            {
                Console.WriteLine("Unkown text encounterd in " + pNode_.Name + ": " + text);
            }
        }
        private string randomString()
        {
           
            StringBuilder sb = new StringBuilder();
            string result;
            do
            {
                int ccount = rnd.Next(3, 10);
                sb.Clear();
                while (ccount-- > 0)
                {
                    sb.Append(Convert.ToChar(rnd.Next(0, 25) + 65));
                }
                result = sb.ToString();
            } while (randoms.Contains(result));
            return result;
        }
        private string scrubText(string text)
        {
            string original = text;
            string replacement;
            if (text.Length == 0) return text;

            MatchCollection res = DIR_PATTERN.Matches(text);
            if (res.Count == 0)
            {
                if (pathMap_.TryGetValue(original, out replacement))
                {
                    return replacement;
                }
                replacement = randomString();
                pathMap_.Add(original, replacement);
                randoms.Add(replacement);
                return replacement;
            }
            
            replacement = "SCRUBBED";
            StringBuilder scrubdir = new StringBuilder();
            List<string> replacements = new List<string>();
            foreach (Match m in res)
            {
                if (!m.Success)
                {
                    continue;
                }
                else
                {
                    original = m.Result("${pathelem}");
                }
                if (pathMap_.TryGetValue(original, out replacement))
                {
                    replacements.Add(replacement);
                }
                else
                {
                    replacement = randomString();
                    pathMap_.Add(original, replacement);
                    randoms.Add(replacement);
                }
                scrubdir.Append(replacement).Append("/");
            }
            Match fmatch = FDIR_PATTERN.Match(text);
            if (fmatch.Success)
            {
                original = fmatch.Result("${fname}");

                if (pathMap_.TryGetValue(original, out replacement))
                {
                    replacements.Add(replacement);
                }
                else
                {
                    replacement = randomString();
                    pathMap_.Add(original, replacement);
                    randoms.Add(replacement);
                }
                scrubdir.Append(replacement).Append(fmatch.Result("${ext}"));
            }
            return scrubdir.ToString();
        }

        public XmlNode Get()
        {
            return rootNode;
        }
    }
}
