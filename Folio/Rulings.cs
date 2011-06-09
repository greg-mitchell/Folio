using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Folio
{
	public static class Rulings
	{		
			
		// fields
		private static BackgroundWorker bgWorker;
		private static List<CardRuling> cards;
		
		#region application settings fields
		private static Uri rulingSource = new Uri( "http://www.crystalkeep.com/magic/rules/oracle/oracle-all.txt");
		private static Uri rulingCache = new Uri(new Uri(AppDomain.CurrentDomain.BaseDirectory), "rulings.xml");
		private static DateTime dateCacheExpires = DateTime.Now;
		private static int expirationInterval = 30;
		
		public static DateTime DateCacheExpires 
		{
			get { return dateCacheExpires; }
			set { dateCacheExpires = value; }
		}
		
		public static int ExpirationInterval
		{
			get { return expirationInterval;}
			set { expirationInterval = value; }
		}
		
		public static Uri RulingSource 
		{ 
			get { return rulingSource;}
			set { rulingSource = value;}
		}
		
		public static Uri RulingsCache
		{
			get { return rulingCache;}
			set { rulingCache = value; }
		}
		#endregion
		
		public static void GetCardRulings(Filter filter, RunWorkerCompletedEventHandler completedDelegate)
		{
			Console.WriteLine("Call to GetCardInfo");

            if(bgWorker == null || !bgWorker.IsBusy)
            {
                bgWorker = new BackgroundWorker();
                bgWorker.DoWork += AsyncFindMatches;
                bgWorker.RunWorkerCompleted += completedDelegate;
                bgWorker.RunWorkerAsync(filter);
            }
		}
		
		private static void AsyncFindMatches(object sender, DoWorkEventArgs e)
		{
			try
			{

                BackgroundWorker worker = sender as BackgroundWorker;
                if (cards == null)
                {
                    LoadCache();
                }
				
				// search cache for cardname
				Filter fil = (Filter)e.Argument;
                var matches = cards.Where(card => card.Name.ToUpper().Contains(fil.CardName.ToUpper()));
                                   
                if (fil.Colors != null) matches.Where(card => card.Color == fil.Colors);
				if(fil.Types != null)   matches.Where(card => card.MatchesType(fil.Types));

                //matches.Select<CardRuling,Card>(cr =>
                //{
                //    Card c = new Card() { Name = cr.Name, Type = cr.Type };
                //    c.TrySetCostAndColor(cr.Cost.ToString());
                //    return c;
                //});

                e.Result = matches;
			}
			catch (InvalidCastException ex)
            {
                Console.WriteLine("Invalid cast!  Ensure data object is type correct\nMessage:\n{0}", ex.Message);
            }
            catch (ThreadAbortException ex)
            {
                Console.WriteLine("Aborting SyncGrabInfo thread!\nMessage:{0}", ex.Message);
            }
		}
		
		public static void LoadCache(bool forceReload=false)
		{
			// if the cache is null or has expired, request a new copy
            if (forceReload
               || !File.Exists(RulingsCache.ToString())
               || DateTime.Now.CompareTo(DateCacheExpires) <= 0)
            {
                WebRequest request = null;
                WebResponse response = null;
                try
                {
                    request = HttpWebRequest.Create(RulingSource);
                    response = request.GetResponse();

                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        cards = ParseRulings(sr);
                    }

                    XmlSerializer ser = new XmlSerializer(cards.GetType());
                    using (StreamWriter sw = new StreamWriter(RulingsCache.ToString(), false))
                    {
                        ser.Serialize(sw, cards);
                    }

                    DateCacheExpires = DateTime.Now.AddDays(ExpirationInterval);

                }
                catch (ThreadAbortException ex)
                {
                    Console.WriteLine("Closing WebRequests in LoadCache");

                }
            }
                // else deserialize the existing cache
            else
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<CardRuling>));
                using(StreamReader sr = new StreamReader(RulingsCache.ToString()))
                {
                    cards = (List<CardRuling>) ser.Deserialize(sr);
                }
            }
		}
		
		/// <summary>
		/// Parses a stream over a rulings file into a list of cards.
		/// Does not close the stream!
		/// </summary>
		/// <param name="sr">
		/// A <see cref="StreamReader"/>
		/// </param>
		/// <returns>
		/// A List of CardRulings
		/// </returns>
		private static List<CardRuling> ParseRulings(StreamReader inputStream)
		{
            Stopwatch sw = new Stopwatch();
            Stopwatch swTotal = new Stopwatch();
            int index = 0;
            long readLineCount = 0;
            long trySetCount = 0;
            long parseTypesCount = 0;
            long addRulesTextCount = 0;


			List<CardRuling> parsedRulings = new List<CardRuling>();
			List<string> lineBuf = new List<string>(10);
			string line;

            sw.Start(); swTotal.Start();
			while((line = inputStream.ReadLine()) != null)
			{
                readLineCount += sw.ElapsedMilliseconds;
                sw.Reset();
                index++;
                if (index % 1000 == 0)
                {
                    Console.WriteLine("Action\t\t   Avg. Time @ {5}\n" +
                        "ReadLine\t\t   {0}\n" +
                        "TrySetCost\t\t {1}\n" +
                        "ParseTypes\t\t {2}\n" +
                        "AddRulesT\t\t  {3}\n" +
                        "TOTAL:\t\t     {4}", new object[] { readLineCount / index, trySetCount / index, parseTypesCount / index, addRulesTextCount / index , (readLineCount+trySetCount+parseTypesCount+addRulesTextCount)/index, index});
                    Console.WriteLine("Time Elapsed @ {0}:\t{1}", new object[] { index, sw.ElapsedMilliseconds });
                }
				// parse card by card
				if(line != "")
					lineBuf.Add(line);
				else if(lineBuf.Count >= 2)
				{

					int i = 0;
					CardRuling newCard = new CardRuling();
					newCard.Name = lineBuf[i++];

                    sw.Start();
					if(newCard.TrySetCostAndColor(lineBuf[i]))
						i++;
                    trySetCount += sw.ElapsedMilliseconds;
                    sw.Reset();

                    sw.Start();
					newCard.Type = Card.ParseTypes(lineBuf[i++]);
                    parseTypesCount += sw.ElapsedMilliseconds;
                    sw.Reset();

                    sw.Start();
					for( ; i<lineBuf.Count; i++)
					{
						newCard.RulesText.Add(lineBuf[i]);
					}
                    addRulesTextCount += sw.ElapsedMilliseconds;
                    sw.Reset();

					parsedRulings.Add(newCard);
					lineBuf.Clear();
				}

                sw.Start();
			}
			
			return parsedRulings;
		}
		
		private static void FastWriteCache(StreamReader inputStream)
		{
			using (XmlWriter xw = 
		       XmlWriter.Create(
		                        new XmlTextWriter(new StreamWriter(new FileStream(RulingsCache.AbsolutePath, FileMode.Create))),
					            new XmlWriterSettings() { Indent = true }))
			{
				List<string> lineBuf = new List<string>(10);	
				xw.WriteStartDocument();
				xw.WriteElementString("Expiration",DateTime.Now.AddDays(ExpirationInterval).ToShortDateString());
				xw.WriteStartElement("CardRulings", "Folio");
				string line;
				while((line = inputStream.ReadLine()) != null)
				{
					
					// parse card by card
					if(line != "")
						lineBuf.Add(line);
					else
					{
						int i = 0;
						CardRuling newCard = new CardRuling();
						newCard.Name = lineBuf[i++];
						if(newCard.Cost.TrySetCost(lineBuf[i]))
							i++;
						
						newCard.Type = Card.ParseTypes(lineBuf[i++]);
						for( ; i<lineBuf.Count; i++)
						{
							newCard.RulesText.Add(lineBuf[i]);
						}
						
						xw.WriteStartElement("Card");
						xw.WriteAttributeString("Name",newCard.Name);
						xw.WriteAttributeString("Cost",newCard.Cost.ToString());
						xw.WriteAttributeString("Types",newCard.Type.ToString());
						
						foreach(string l in newCard.RulesText)
							xw.WriteString(l);
						
						xw.WriteEndElement();
					}
				}
				xw.WriteEndElement();
				xw.WriteEndDocument();
			}
		}
			
		
	}

    
}

