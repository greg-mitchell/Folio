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
	/// Represents a basic Magic: The Gathering card.
	/// </summary>
    [Serializable]
    public class Card
    {
		private Cost _cost;
		
		[XmlAttribute]
        public string Name { get; set; }
		
		[XmlAttribute]
        public CardColors Color { get; set; }
        
		[XmlAttribute]
		public CardTypes Type { get; set; }
			
        [XmlElement]
		public Cost Cost
		{ 
            get { return _cost;}
            set { _cost = value; }
        }
		
		public Card ()
		{
			_cost = new Cost();
		}
		
		public void SetCostAndColor(Cost cost)
		{
			this._cost = cost;
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
            return this._cost.TrySetCost(cost);
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

    /// <summary>
    /// Represents a (non-physical) card ruling
    /// Contains each line of oracle text in a List<string>
    /// </summary>
    public class CardRuling : Card
    {
        [XmlArrayItem("RulesLine")]
        public List<string> RulesText { get; set; }

        public CardRuling()
        {
            RulesText = new List<string>();
        }
    }

    /// <summary>
    /// Represents a set physical cards in a collection.
    /// The cards have the same set, condition, etc
    /// </summary>
    public class CardCollectionCard : Card
    {
        [XmlElement]
        public int Quantity { get; set; }

        [XmlElement]
        public string Set { get; set; }

        [XmlElement]
        public Condition Condition { get; set; }

        [XmlElement]
        public string Notes { get; set; }

        public static explicit operator CardCollectionCard(CardRuling cr)
        {
            CardCollectionCard ccc = new CardCollectionCard() { Quantity = 1, Condition=Condition.NM };
            ccc.Name = cr.Name;
            ccc.Cost = cr.Cost;
            ccc.Color = cr.Color;
            ccc.Type = cr.Type;
            return ccc;
        }
    }
}
