using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using Microsoft.AspNetCore.Mvc;

namespace XMLtoJsonApi.Controllers
{
    public class ValuesController : Controller
    {
     
        // GET api/values/5
        public JsonResult Get(string argument)
        {
            string url = HttpUtility.UrlDecode(argument);
            string result = null;
            try
            {
                result = GetXML(url);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(result);

                result = XmlToJson(xml);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            var resp = new JsonResult(result);
            return resp;

        }

        private string GetXML(string url)
        {
            string result = null;
            try
            {
                if (url != null)
                {
                    if (WebRequest.Create(url) is HttpWebRequest req)
                    {
                        req.Timeout = 500000000;

                        if (req.GetResponse() is HttpWebResponse resp)
                        {
                            var reader =
                                new StreamReader(resp.GetResponseStream() ?? throw new InvalidOperationException());
                            result = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            return result;
        }

        private static string XmlToJson(XmlDocument xmlDoc)
        {
            StringBuilder sbJson = new StringBuilder();
            sbJson.Append("{ ");
            XmlToJsonNode(sbJson, xmlDoc.DocumentElement, true);
            sbJson.Append("}");
            return sbJson.ToString();
        }

        //  XmlToJSON node:  Output an XmlElement, possibly as part of a higher array
        private static void XmlToJsonNode(StringBuilder sbJson, XmlElement node, bool showNodeName)
        {
            if (showNodeName)
                sbJson.Append("\"" + SafeJson(node.Name) + "\": ");
            sbJson.Append("{");
            // Build a sorted list of key-value pairs
            //  where   key is case-sensitive nodeName
            //          value is an ArrayList of string or XmlElement
            //  so that we know whether the nodeName is an array or not.
            SortedList childNodeNames = new SortedList();

            //  Add in all node attributes
            if (node.Attributes != null)
                foreach (XmlAttribute attr in node.Attributes)
                    StoreChildNode(childNodeNames, attr.Name, attr.InnerText);

            //  Add in all nodes
            foreach (XmlNode cNode in node.ChildNodes)
            {
                if (cNode is XmlText)
                    StoreChildNode(childNodeNames, "value", cNode.InnerText);
                else if (cNode is XmlElement)
                    StoreChildNode(childNodeNames, cNode.Name, cNode);
            }

            // Now output all stored info
            foreach (string childName in childNodeNames.Keys)
            {
                ArrayList alChild = (ArrayList)childNodeNames[childName];
                if (alChild.Count == 1)
                    OutputNode(childName, alChild[0], sbJson, true);
                else
                {
                    sbJson.Append(" \"" + SafeJson(childName) + "\": [ ");
                    foreach (object child in alChild)
                        OutputNode(childName, child, sbJson, false);
                    sbJson.Remove(sbJson.Length - 2, 2);
                    sbJson.Append(" ], ");
                }
            }
            sbJson.Remove(sbJson.Length - 2, 2);
            sbJson.Append(" }");
        }

        //  StoreChildNode: Store data associated with each nodeName
        //                  so that we know whether the nodeName is an array or not.
        private static void StoreChildNode(SortedList childNodeNames, string nodeName, object nodeValue)
        {
            // Pre-process contraction of XmlElement-s
            if (nodeValue is XmlElement xmlNode)
            {
                // Convert  <aa></aa> into "aa":null
                //          <aa>xx</aa> into "aa":"xx"
                XmlNode cNode = xmlNode;
                if (cNode.Attributes.Count == 0)
                {
                    XmlNodeList children = cNode.ChildNodes;
                    if (children.Count == 0)
                        nodeValue = null;
                    else if (children.Count == 1 && (children[0] is XmlText))
                        nodeValue = ((XmlText)(children[0])).InnerText;
                }
            }
            // Add nodeValue to ArrayList associated with each nodeName
            // If nodeName doesn't exist then add it
            object oValuesAl = childNodeNames[nodeName];
            ArrayList valuesAl;
            if (oValuesAl == null)
            {
                valuesAl = new ArrayList();
                childNodeNames[nodeName] = valuesAl;
            }
            else
                valuesAl = (ArrayList)oValuesAl;
            valuesAl.Add(nodeValue);
        }

        private static void OutputNode(string childName, object alChild, StringBuilder sbJson, bool showNodeName)
        {
            if (alChild == null)
            {
                if (showNodeName)
                    sbJson.Append("\"" + SafeJson(childName) + "\": ");
                sbJson.Append("null");
            }
            else if (alChild is string sChild)
            {
                if (showNodeName)
                    sbJson.Append("\"" + SafeJson(childName) + "\": ");
                sChild = sChild.Trim();
                sbJson.Append("\"" + SafeJson(sChild) + "\"");
            }
            else
                XmlToJsonNode(sbJson, (XmlElement)alChild, showNodeName);
            sbJson.Append(", ");
        }

        // Make a string safe for JSON
        private static string SafeJson(string sIn)
        {
            StringBuilder sbOut = new StringBuilder(sIn.Length);
            foreach (char ch in sIn)
            {
                if (char.IsControl(ch) || ch == '\'')
                {
                    int ich = ch;
                    sbOut.Append(@"\u" + ich.ToString("x4"));
                    continue;
                }

                if (ch == '\"' || ch == '\\' || ch == '/')
                {
                    sbOut.Append('\\');
                }
                sbOut.Append(ch);
            }
            return sbOut.ToString();
        }
    }
}