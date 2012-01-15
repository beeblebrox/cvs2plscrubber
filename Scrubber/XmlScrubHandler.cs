using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlManager;

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

        private static SortedSet<string> SKIPPED_NODES = null;
        private static SortedSet<string> EMPTIED_NODES = null;
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
            unknownTags_ = new SortedSet<string>();
            parentNodeStack_ = new Stack<XmlNode>();

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
            if (SKIPPED_NODES.Contains(qName) || EMPTIED_NODES.Contains(qName))
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

            //TODO, add option to wipe unkown text or ignore it.
            //This is also a failsafe and only prints if the tag was known at the time of
            //writting this but unknow that it could contain text.
            if (!unknownTags_.Contains(wNode_.Name))
            {
                Console.WriteLine("Unkown text encounterd in " + pNode_.Name + ": " + text);
            }
        }

        public XmlNode Get()
        {
            return rootNode;
        }
    }
}
