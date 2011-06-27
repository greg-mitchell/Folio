using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Folio
{
    /// <summary>
    /// A source-independent filter for cards
    /// Default source is magiccards.info
    /// </summary>
    public class Filter
    {
        private Dictionary<string, string> setDictionary;
        private string set;
        private string setAbbreviation;

        public enum Sources {
            magiccards_dot_info
        }

        public enum IsCriteria
        {
            // list of criteria for is: query
        }

		public CardColors? Colors { get; set; }
        public bool Multicolor { get; private set; }
        public bool Colorless { get; private set; }
        public string CardName { get; set; }
        public string RulesText { get; set; }
        public CardTypes? Types { get; set; }
			
		public bool W
		{
			get { return (Colors & CardColors.W) != 0; }
			set
			{
				if(value)
					Colors |= CardColors.W;
				else
					Colors &= ~CardColors.W;
			}
		}
		public bool U
		{
			get { return (Colors & CardColors.U) != 0; }
			set
			{
				if(value)
					Colors |= CardColors.U;
				else
					Colors &= ~CardColors.U;
			}
		}
		public bool B
		{
			get { return (Colors & CardColors.B) != 0; }
			set
			{
				if(value)
					Colors |= CardColors.B;
				else
					Colors &= ~CardColors.B;
			}
		}
		public bool R
		{
			get { return (Colors & CardColors.R) != 0; }
			set
			{
				if(value)
					Colors |= CardColors.R;
				else
					Colors &= ~CardColors.R;
			}
		}
		public bool G
		{
			get { return (Colors & CardColors.G) != 0; }
			set
			{
				if(value)
					Colors |= CardColors.G;
				else
					Colors &= ~CardColors.G;
			}
		}
		
        public bool SetMulticolor(bool value)
        {
            // no issues if setting Multicolor to be false or if Colorless is not checked
            if (!value || !Colorless)
            {
                Multicolor = value;
                return value;
            }

            // otherwise, setting to true and Colorless is true, so return false
            return false;
        }

        public bool SetColorless(bool value)
        {
            // no issues if setting Multicolor to be false or if Colorless is not checked
            if (!value || !Multicolor)
            {
                Colorless = value;
                return value;
            }

            // otherwise, setting to true and Colorless is true, so return false
            return false;
        }

        public string GetSearchQuery()
        {
            return "";
        }

        public void SetSearchQuery(string value)
        {
            // TODO some regex
        }

        public string Set
        {
            get { return set; }
            set
            {
                string abbr;
                if (setDictionary.TryGetValue(value, out abbr))
                {
                    setAbbreviation = abbr;
                }
                else throw new ArgumentException(String.Format("\"{0}\" is not a valid Set name",value));
            }
        }

        public string SetAbbreviation
        {
            get { return setAbbreviation; }
            set
            {
                string set;
                if (setDictionary.TryGetValue(value, out set))
                    this.set = set;
                else throw new ArgumentException(String.Format("\"{0}\" is not a valid Set abbreviation", value));
            }
        }

        // default
        public Uri GetUri() { return GetUri(Sources.magiccards_dot_info); }

        public Uri GetUri(Sources source)
        {
            Uri uri;
            switch (source)
            {
                case Sources.magiccards_dot_info:
                    uri = BuildMagiccardsUri();
                    break;
                default:
                    uri = BuildMagiccardsUri();
                    break;
            }
            return uri;
        }

        private Uri BuildMagiccardsUri()
        {
            string q = "http://magiccards.info/query?q=";
            q += CardName.Replace(' ', '+');  // might have to sanitize
            
            // set rules text
            if (RulesText != null && RulesText.Length > 0)
            {
                q += "+o:\"" + RulesText + "\"";
            }

            // set type
            if (Types != null)
                q += "+t:\"" + TypesToString(Types) + "\"";

            // set color
            if(!(Colors == null || Colors == CardColors.Colorless) || Multicolor || Colorless)
            {
                q += "+c!";
                q += ((Colorless) ? "c" : "");
                q += ((Multicolor) ? "m" : "");
                q += (((Colors & CardColors.W) != 0) ? "w" : "");
                q += (((Colors & CardColors.U) != 0) ? "u" : "");
                q += (((Colors & CardColors.B) != 0) ? "b" : "");
                q += (((Colors & CardColors.R) != 0) ? "r" : "");
                q += (((Colors & CardColors.G) != 0) ? "g" : "");
            }

            // set set
            if (!String.IsNullOrEmpty(SetAbbreviation))
                q += "+e:" + SetAbbreviation;

            return new Uri(q);
        }
		
		private string TypesToString(CardTypes? t)
		{
			
			StringBuilder sb = new StringBuilder();
			foreach(CardTypes type in Enum.GetValues(typeof(CardTypes)))
			{
				sb.Append(type.ToString() + " ");
			}
			return sb.ToString().Trim();
		}
    }
}
