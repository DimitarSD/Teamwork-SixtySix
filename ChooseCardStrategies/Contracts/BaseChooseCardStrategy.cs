namespace Santase.AI.NinjaPlayer.ChooseCardStrategies.Contracts
{
    using System.Collections.Generic;
    using System.Linq;
    using Logic.Players;
    using Logic.Cards;
    using Helpers;
    using Logic;

    public abstract class BaseChooseCardStrategy : IChooseCardStrategy
    {
        protected ICollection<Card> possibleCardsToPlay;
        protected CardTracker cardTracker;
        protected CardValidator cardValidator;

        protected BaseChooseCardStrategy(ICollection<Card> possibleCardsToPlay, CardTracker cardTracker, CardValidator cardValidator)
        {
            this.possibleCardsToPlay = possibleCardsToPlay;
            this.cardTracker = cardTracker;
            this.cardValidator = cardValidator;
        }

        public abstract PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> cards);

        protected PlayerAction PlayCard(ICollection<Card> cards, Card card)
        {
            cards.Remove(card);
            return PlayerAction.PlayCard(card);
        }

        protected Card GetSmallestNonTrumpCard()
        {
            return this.possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit != this.cardTracker.TrumpSuit);
        }

        protected Card GetSmallestNonAnnounceNonTrumpCard(PlayerTurnContext context)
        {
            return this.possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit != this.cardTracker.TrumpSuit
                && !this.cardValidator.IsCardInAnnounce(context, c, this.possibleCardsToPlay, Announce.Twenty));
        }

        protected Card GetSmallestTrumpCard()
        {
            return this.possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);
        }

        protected Card GetSmallestNonAnnounceTrumpCard(PlayerTurnContext context)
        {
            return this.possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit
                && !this.cardValidator.IsCardInAnnounce(context, c, this.possibleCardsToPlay, Announce.Forty));
        }

        protected Card GetSmallestCard()
        {
            return this.possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault();
        }

        protected Card GetHigherCard(Card firstPlayedCard)
        {
            return this.possibleCardsToPlay.OrderByDescending(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == firstPlayedCard.Suit && c.GetValue() > firstPlayedCard.GetValue());
        }

        protected Card GetHighestCardInSuit(ICollection<Card> cards, CardSuit suit)
        {
            return cards.OrderByDescending(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == suit);
        }

        protected CardSuit GetOpponentsShortestSuit()
        {
            return this.cardTracker.RemainingCards
                .GroupBy(x => x.Suit)
                .OrderBy(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(s => s != this.cardTracker.TrumpSuit);
        }

        protected PlayerAction AnnounceMarriage(PlayerTurnContext context, ICollection<Card> cards)
        {
            if (context.State.CanAnnounce20Or40)
            {
                // get 40
                var card = this.GetAnnounce(context, Announce.Forty, cards);

                if (card != null)
                {
                    return this.PlayCard(cards, card);
                }

                // get 20
                card = this.GetAnnounce(context, Announce.Twenty, cards);

                if (card != null)
                {
                    return this.PlayCard(cards, card);
                }
            }

            return null;
        }

        private Card GetAnnounce(PlayerTurnContext context, Announce announce, ICollection<Card> cards)
        {
            foreach (var card in possibleCardsToPlay)
            {
                if (this.cardValidator.IsCardInAnnounce(context, card, cards, announce))
                {
                    return card;
                }
            }

            return null;
        }
    }
}
