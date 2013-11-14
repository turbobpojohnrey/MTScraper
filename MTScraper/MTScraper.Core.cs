using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Net;
using System.IO;
using HtmlAgilityPack;

namespace MTScraper
{
    public partial class Core
    {
        //protected BackgroundWorker _worker;

        #region <-Declarations

        protected Semaphore _pool = null;
        protected BackgroundWorker _worker = null;

        #endregion

        public Core(int Initial, int Maximum)
        {
            this._pool = new Semaphore(Initial, Maximum);
        }

        /*
         * 
         * Used to scrape page source with specific paramaters:
         * 
         * String url:                  the target url
         * Action<String> callback:   a parameterized method to call after _pageSource method is call internally
         * 
         * */
        public void getSource(String url, Action<String> callback)
        {
            //initiate a request worker
            this._worker = new BackgroundWorker();

            //set server request method
            this._worker.DoWork += new DoWorkEventHandler((sender, e) =>
            {
                this._pool.WaitOne();
                Uri uri = new Uri(url);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.BeginGetResponse(_getSource, new object[] { request, callback });
            });

            //start server request
            this._worker.RunWorkerAsync();
        }

        private void _getSource(IAsyncResult ir)
        {
            object[] args = (object[])ir.AsyncState;
            Action<String> callBack = (Action<String>)args[1];

            HttpWebRequest request = (HttpWebRequest)args[0];

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(ir))
                {
                    ResponseReader reader = new ResponseReader(response);
                    String pageSource = reader.read();

                    if (callBack != null)
                        callBack(pageSource);
                }
            }
            finally
            {
                try
                {
                    _pool.Release();
                }
                catch { }
            }
        }

        //Get Image Async

        public void getImage(String url, String filename, Action<Boolean, String> callBack)
        {
            //initiate a request worker
            this._worker = new BackgroundWorker();

            //set server request method
            this._worker.DoWork += new DoWorkEventHandler((sender, e) =>
            {
                try
                {
                    WebClient wc = new WebClient();
                    Uri uri = new Uri(url);
                    wc.DownloadFileAsync(uri, filename);
                }
                catch
                {
                    callBack(false, filename);
                }
            });


            this._worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((sender, e) => {
                callBack(true, filename);
            });

            //start server request
            this._worker.RunWorkerAsync();

        }

        public void destroy()
        {

            try
            {
                this._worker.CancelAsync();
                this._worker = null;
            }
            catch { }

            //clear the pool
            try
            {
                this._pool.Dispose();
                this._pool.Close();
                this._pool = null;
            }
            catch { }

        }

    }

    /// <summary>
    /// Reference: HtmlAgilityPack
    /// Creates a DOM based on pageSource parameter
    /// </summary>
    public partial class HtmlDOM
    {
        private HtmlAgilityPack.HtmlDocument dom;

        public HtmlDOM(String pageSource)
        {
            dom = new HtmlDocument();
            dom.LoadHtml(pageSource);
        }

        public HtmlNode getNode(String xPath)
        {
            var node = this.dom.DocumentNode.SelectSingleNode(xPath);
            return node;
        }

        public HtmlNodeCollection getNodes(String xPath)
        {
            var nodes = this.dom.DocumentNode.SelectNodes(xPath);
            return nodes;
        }

        public String getNodeHtml(HtmlNode node)
        {
            try
            {
                return node.InnerHtml;
            }
            catch { throw new Exception(); }
        }

        public String getAttribute(HtmlNode node, String nodeAttribute, String defValue)
        {
            try
            {
                return node.GetAttributeValue(nodeAttribute, defValue);
            }
            catch { throw new Exception(); }
        }

    }

    /*
     * 
     * The response reader, used to read the page source of the response object and return as string:
     * 
     * Parameters:
     * 
     * HttpWebResponse response: the response object
     * 
     * */

    public partial class ResponseReader
    {
        StreamReader sr;
        public ResponseReader(HttpWebResponse response)
        {
            sr = new StreamReader(response.GetResponseStream());
        }
        public string read()
        {
            String pageSource = sr.ReadToEnd();
            sr.Close();
            return pageSource;
        }
    }

}
