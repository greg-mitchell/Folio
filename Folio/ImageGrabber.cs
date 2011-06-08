using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using HTML;
using System.Diagnostics;
using System.ComponentModel;

namespace Folio
{
    public delegate void UpdateImageDelegate(string newImageUri);
    
    class ImageGrabber
    {
        private static BackgroundWorker bgWorker;
	
        public static void GetImageUrl(Filter filter, RunWorkerCompletedEventHandler callback)
        {
            Console.WriteLine("Call to GetImageUrl");
            if (bgWorker == null || !bgWorker.IsBusy)
            {
                bgWorker = new BackgroundWorker();
                bgWorker.DoWork += new DoWorkEventHandler(AsyncGrabImage);
                bgWorker.WorkerSupportsCancellation = true;
                bgWorker.RunWorkerCompleted += callback;
                bgWorker.RunWorkerAsync(filter);
            }
        }

        private static void AsyncGrabImage(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("Starting an async image grab");
            WebRequest request = null;
            WebResponse response = null;
            string page;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                Filter filter = (Filter)e.Argument;

                Uri uri = filter.GetUri(Filter.Sources.magiccards_dot_info);
                request = HttpWebRequest.Create(uri);

                Console.WriteLine("Creating data and request took {0} ms", sw.ElapsedMilliseconds);

                sw.Reset(); sw.Start();
                response = request.GetResponse();
                Console.WriteLine("Getting Response took {0} ms", sw.ElapsedMilliseconds);

                sw.Reset(); sw.Start();
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    page = sr.ReadToEnd();
                    sr.Close();
                }
                Console.WriteLine("Reading page and closing the streams took {0} ms", sw.ElapsedMilliseconds);

                sw.Reset(); sw.Start();
                e.Result = ParsePageForImage(page);
                Console.WriteLine("Parsing the page and setting the ImageLocation took {0} ms", sw.ElapsedMilliseconds);
            }
            catch (InvalidCastException ex)
            {
                Console.WriteLine("Invalid cast!  Ensure data object is type ThreadData\nMessage:\n{0}", ex.Message);
            }
            catch (ThreadAbortException ex)
            {
                Console.WriteLine("Aborting SyncGrabImage thread!\nMessage:{0}", ex.Message);
                if (request != null) request.Abort();
                Console.WriteLine("Aborted web request");
                if (response != null) response.Close();
                Console.WriteLine("Closed response");
                Console.WriteLine("SyncGrabImage aborted gracefully!");
            }
            finally
            {
                Console.WriteLine("Ended an asynchronous image grab");
            }
        }

        private static string ParsePageForImage(string page)
        {
            ParseHTML parse = new ParseHTML();
            parse.Source = page;
            while (!parse.Eof())
            {
                char ch = parse.Parse();
                if (ch == 0)
                {
                    AttributeList tag = parse.GetTag();
                    if (tag["src"] != null && tag.Name == "img" && tag["src"].Value.Contains("scans"))
                    {
                        return tag["src"].Value;
                    }
                }
            }

            return "";
        }
    }
}
