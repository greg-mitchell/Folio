using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace Folio
{
	/// <summary>
	/// Card color bitflag 
	/// </summary>
    [Flags]
    public enum CardColors
    {
		Colorless = 0,
        W = 1,
        U = 2,
        B = 4,
        R = 8,
        G = 16
    }

	/// <summary>
	/// Card types bitflag 
	/// </summary>
    [Flags]
    public enum CardTypes
    {
        Creature = 1,
        Land = 2,
        Artifact = 4,
        Enchantment = 8,
        Sorcery = 16,
        Instant = 32,
        Planeswalker = 64,
        Tribal = 128
    }
	
	/// <summary>
	/// Condition of the physical card 
	/// </summary>
	public enum Condition
	{
        Default,
		/// <summary>Near-mint / mint</summary>
		NM,
		/// <summary>Slightly played</summary>
		SP,
		/// <summary>Played</summary>
		P,
		/// <summary>Heavily played</summary>
		HP
	}

	/// <summary>
	/// Represents a mana cost, as on a card.  Also contains methods for parsing and
	 /// interacting with costs 
	/// </summary>
    [Serializable]
    public class Cost : IComparable<Cost>
    {
		private string regexPattern = @"^(0|[0-9]*[WUBRG]*)$";

        private string cost;
        private int cnc;

        [XmlAttribute(AttributeName = "CostString")]
        public string CostString
        {
            get { return cost; }
            set { cost = value; }
        }

		public Cost () { }

        public Cost(string cost)
        {
            SetCost(cost);
        }

        public int ConvertedCost
        {
            get { return cnc; }
        }

        public string ManaCost
        {
            get { return cost; }
        }
		
		public bool TrySetCost(string cost)
		{
			Regex matcher = new Regex(regexPattern);
            if (!matcher.IsMatch(cost.ToUpper()))
                return false;

            this.cost = cost.ToUpper();

            int cnc = 0;
            string genericCost = "";
            bool parsingGeneric = true;
            for (int i = 0; i < cost.Length; i++)
            {
                int digit;
                if (parsingGeneric && int.TryParse(new string(new char[] { cost[i] }), out digit))
                {
                    genericCost += digit;
                }
                else
                {
                    if (genericCost != "")
                    {
                        parsingGeneric = false;
                        cnc += int.Parse(genericCost);
                        genericCost = "";
                    }

                    cnc++;
                }
            }

            this.cnc = cnc;
			
			return true;
		}

        public void SetCost(string cost)
        {
			Regex matcher = new Regex(regexPattern);
            if (!matcher.IsMatch(cost.ToUpper()))
                throw new ArgumentException("Cost {0} is not in a valid format.",cost);
			
            TrySetCost(cost);
        }
		
		public static CardColors ParseColors(string cost)
		{
			CardColors colors = CardColors.Colorless;
			if(cost.Contains("W")) colors |= CardColors.W;
			if(cost.Contains("U")) colors |= CardColors.U;
			if(cost.Contains("B")) colors |= CardColors.B;
			if(cost.Contains("R")) colors |= CardColors.R;
			if(cost.Contains("G")) colors |= CardColors.G;
			
			return colors;
		}
		public CardColors ParseColors()
		{
			return ParseColors(this.cost);
		}
		

        public override string ToString()
        {
            return cost;
        }

        public int CompareTo(Cost other)
        {
            if (this.cnc < other.cnc) return -1;
            if (this.cnc > other.cnc) return 1;

            return 0;
        }
    }

	/// <summary>
	/// Represents a physical Magic: The Gathering card.
	/// </summary>
    [Serializable]
    public class Card
    {
		private Cost cost;
		
		[XmlAttribute]
        public string Name { get; set; }
		
		[XmlAttribute]
        public CardColors Color { get; set; }
        
		[XmlAttribute]
		public CardTypes Type { get; set; }
		
		[XmlElement]
        public string Set { get; set; }

        [XmlElement]
		public Condition? Condition { get; set; }
		
        [XmlElement]
		public Cost Cost
		{ 
            get { return cost;}
            set { cost = value; }
        }
		
		public Card ()
		{
			cost = new Cost();
		}
		
		public void SetCostAndColor(Cost cost)
		{
			this.cost = cost;
			Color = cost.ParseColors();
		}
		
		public bool MatchesType(CardTypes? types)
		{
			if(types == null) return false;
			
			foreach(CardTypes flag in Enum.GetValues(typeof(CardTypes)))
			{
				if((types & flag) != 0 && (this.Type & flag) == 0)
					return false;
			}
			
			return true;
		}
		
		public static CardTypes ParseTypes(string types)
		{
			CardTypes matchedTypes = (CardTypes)0;
			if(types.Contains(CardTypes.Artifact.ToString()))
				matchedTypes |= CardTypes.Artifact;
			if(types.Contains(CardTypes.Creature.ToString()))
				matchedTypes |= CardTypes.Creature;
			if(types.Contains(CardTypes.Enchantment.ToString()))
				matchedTypes |= CardTypes.Enchantment;
			if(types.Contains(CardTypes.Instant.ToString()))
				matchedTypes |= CardTypes.Instant;
			if(types.Contains(CardTypes.Land.ToString()))
				matchedTypes |= CardTypes.Land;
			
			if(types.Contains(CardTypes.Planeswalker.ToString()))
				matchedTypes |= CardTypes.Planeswalker;
			if(types.Contains(CardTypes.Sorcery.ToString()))
				matchedTypes |= CardTypes.Sorcery;
			
			if(types.Contains(CardTypes.Tribal.ToString()))
				matchedTypes |= CardTypes.Tribal;
			
			return matchedTypes;
		}

        public bool TrySetCostAndColor(string cost)
        {
            this.Color = Cost.ParseColors(cost);
            return this.cost.TrySetCost(cost);
        }

        public void SetColor(string cost)
        {
            this.Color = Cost.ParseColors(cost);
        }

		public override string ToString ()
		{
            return "Card: " + Name;
		}
    }
}
