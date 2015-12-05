namespace Santase.AI.NinjaPlayer.Helpers
{
    using Logic.Players;
    using Logic.Cards;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class CardValidator
    {
        public bool HasNonTrumpCardType(PlayerTurnContext context, ICollection<Card> cards, CardType type)
        {
            return cards.Any(x => x.Suit != context.TrumpCard.Suit && x.Type == type);
        }

        public bool HasTrumpCard(PlayerTurnContext context, ICollection<Card> cards)
        {
            return cards.Any(x => x.Suit == context.TrumpCard.Suit);
        }

        public bool HasTrumpCardType(PlayerTurnContext context, ICollection<Card> cards, CardType type)
        {
            return cards.Any(x => x.Suit == context.TrumpCard.Suit && x.Type == type);
        }

        public bool HasHigherCard(Card firstPlayedCard, ICollection<Card> cards)
        {
            return cards.Any(c => c.Suit == firstPlayedCard.Suit
                                           && c.GetValue() > firstPlayedCard.GetValue());
        }

        public bool HasAnyCardInSuit(ICollection<Card> cards, CardSuit suit)
        {
            return cards.Any(c => c.Suit == suit);
        }

        public bool IsTenOrAce(Card card)
        {
            return card.GetValue() >= 10;
        }

        public bool IsTrump(Card card, CardType trumpCardType)
        {
            return card.Type == trumpCardType;
        }
    }
}
