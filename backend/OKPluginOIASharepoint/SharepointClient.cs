using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace OKPluginOIASharepoint
{
    public class SharepointClient
    {
        private readonly string siteDomain;
        private readonly string site;
        private readonly AccessTokenGetter accessTokenGetter;

        public SharepointClient(string siteDomain, string site, AccessTokenGetter accessTokenGetter)
        {
            this.siteDomain = siteDomain;
            this.site = site;
            this.accessTokenGetter = accessTokenGetter;
        }

        public async IAsyncEnumerable<(Guid itemGuid, ExpandoObject data)> GetListItems(Guid listID, string[] columns)
        {
            var itemXml = new XmlDocument();
            try
            {
                var accessToken = await accessTokenGetter.GetAccessTokenAsync();

                var maxItems = 10000; // TODO: find better way... paginate?

                // TODO: rewrite to use flurl, maybe also use JSON instead of XML
                // TODO: only request the columns that are specified, not all
                HttpWebRequest itemRequest = (HttpWebRequest)WebRequest.Create($"https://{siteDomain}/sites/{site}/_api/Web/lists(guid'{listID}')/Items?$top={maxItems}");
                itemRequest.Method = "GET";
                itemRequest.Accept = "application/atom+xml";
                itemRequest.ContentType = "application/atom+xml;type=entry";
                itemRequest.Headers.Add("Authorization", "Bearer " + accessToken);
                HttpWebResponse itemResponse = (HttpWebResponse)await itemRequest.GetResponseAsync();
                StreamReader itemReader = new StreamReader(itemResponse.GetResponseStream());
                itemXml.LoadXml(await itemReader.ReadToEndAsync());
            }
            catch (WebException)
            {
                throw;
            }

            // TODO: rewrite to use XElement
            XmlNamespaceManager xmlnspm = new XmlNamespaceManager(new NameTable());
            xmlnspm.AddNamespace("atom", "http://www.w3.org/2005/Atom");
            xmlnspm.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
            xmlnspm.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");

            var itemList = itemXml.SelectNodes("//atom:entry/atom:content/m:properties", xmlnspm);

            foreach (XmlNode? item in itemList)
            {
                if (item != null)
                {
                    // always add GUID for the item:
                    // the GUID of line items stays constant when the item is changed
                    var guid = item.SelectSingleNode("d:GUID", xmlnspm);

                    var r = ParseXMLItemNode(item, columns, xmlnspm);

                    yield return (new Guid(guid.InnerText), r);
                }
            }
        }

        public async Task<ExpandoObject> GetListItem(Guid listID, Guid itemID, string[] columns)
        {
            var itemXml = new XmlDocument();
            try
            {
                var accessToken = accessTokenGetter.GetAccessToken();

                // TODO: rewrite to use flurl, maybe also use JSON instead of XML
                HttpWebRequest itemRequest = (HttpWebRequest)WebRequest.Create($"https://{siteDomain}/sites/{site}/_api/Web/lists(guid'{listID}')/Items?$filter=GUID eq '{itemID}'");
                itemRequest.Method = "GET";
                itemRequest.Accept = "application/atom+xml";
                itemRequest.ContentType = "application/atom+xml;type=entry";
                itemRequest.Headers.Add("Authorization", "Bearer " + accessToken);
                HttpWebResponse itemResponse = (HttpWebResponse)await itemRequest.GetResponseAsync();
                StreamReader itemReader = new StreamReader(itemResponse.GetResponseStream());
                itemXml.LoadXml(await itemReader.ReadToEndAsync());
            }
            catch (WebException)
            {
                throw;
            }

            // TODO: rewrite to use XElement
            XmlNamespaceManager xmlnspm = new XmlNamespaceManager(new NameTable());
            xmlnspm.AddNamespace("atom", "http://www.w3.org/2005/Atom");
            xmlnspm.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
            xmlnspm.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");

            var itemList = itemXml.SelectNodes("//atom:entry/atom:content/m:properties", xmlnspm);

            if (itemList.Count == 0)
            {
                // did not find item in list
                return new ExpandoObject();
            }
            else if (itemList.Count > 1)
            {
                // ??? possible? TODO: handle this case
            }

            var item = itemList.Item(0);
            var r = ParseXMLItemNode(item, columns, xmlnspm);

            return r;
        }

        private ExpandoObject ParseXMLItemNode(XmlNode? itemNode, string[] columns, XmlNamespaceManager xmlnspm)
        {
            var r = new ExpandoObject();
            foreach (var column in columns)
            {
                var node = itemNode?.SelectSingleNode($"d:{column}", xmlnspm);
                if (node != null)
                    r.TryAdd(column, node.InnerText);
            }
            return r;
        }
    }
}
