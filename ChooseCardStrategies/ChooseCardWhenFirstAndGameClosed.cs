namespace Santase.AI.NinjaPlayer.ChooseCardStrategies
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.NinjaPlayer.ChooseCardStrategies.Contracts;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using Helpers;

    public class ChooseCardWhenFirstAndGameClosed : BaseChooseCardStrategy, IChooseCardStrategy
    {
        public ChooseCardWhenFirstAndGameClosed(ICollection<Card> possibleCardsToPlay, CardTracker cardTracker, CardValidator cardValidator)
            : base(possibleCardsToPlay, cardTracker, cardValidator)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> cards)
        {
            Card card;

            // play sure card first
            var sureCards = context.CardsLeftInDeck > 0 
                ? this.cardTracker.GetSureCardsWhenGameClosed(context, this.possibleCardsToPlay)
                : this.cardTracker.GetSureCardsWhenGameClosed(context, this.possibleCardsToPlay, false);

            var test = this.possibleCardsToPlay;
            var asd = this.cardTracker.RemainingCards;
            if (sureCards.Count > 0)
            {
                card = sureCards.FirstOrDefault(c => c.Suit != this.cardTracker.TrumpSuit);

                if (card == null)
                {
                    card = sureCards.First();
                }

                return this.PlayCard(cards, card);
            }

            // announce marriage
            var announce = AnnounceMarriage(context, cards);
            if (announce != null)
            {
                return announce;
            }

            // if player is close to the win & opponent has no higher trump card => play trump card
            card = this.possibleCardsToPlay.OrderByDescending(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

            if (card != null &&
                (!this.cardValidator.HasTrumpCard(context, this.cardTracker.RemainingCards)
                || this.GetHighestCardInSuit(this.cardTracker.RemainingCards, this.cardTracker.TrumpSuit).GetValue() < card.GetValue())
                && this.cardTracker.MyTrickPoints + card.GetValue() >= GlobalConstants.EnoughPointsToWinGame)
            {
                return this.PlayCard(cards, card);
            }

            // try to make opponent play trump cards
            card = this.ControlTrumpWhenGameClosed(this.cardTracker.TrumpSuit);
            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            if (context.CardsLeftInDeck > 0)
            {
                // risk and play high card when no more than two of the same suit have already been played
                card = this.GetPossibleSureCardsWhenGameClosed();
                if (card != null)
                {
                    return this.PlayCard(cards, card);
                }
            }

            card = this.GetSmallestNonTrumpCard();
            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            return this.PlayCard(cards, this.GetSmallestCard());
        }

        private Card ControlTrumpWhenGameClosed(CardSuit trumpSuit)
        {
            var myTrumpCards = this.cardTracker.MyRemainingTrumpCards;
            var opponentsTrumpCards = this.cardTracker.RemainingCards.Where(c => c.Suit == trumpSuit).ToList();

            if (opponentsTrumpCards.Count == 0)
            {
                return null;
            }

            // try to find suit from which opponent has no cards and play it to make opponent play trump
            foreach (var myCard in this.possibleCardsToPlay.OrderBy(c => c.GetValue()))
            {
                if (!this.cardValidator.HasAnyCardInSuit(this.cardTracker.RemainingCards, myCard.Suit) 
                    && myCard.Suit != trumpSuit && myCard.GetValue() < 10)
                {
                    return myCard;
                }
            }

            // if player has more trump cards play small trump card to make opponent play trump
            if (myTrumpCards.Count > opponentsTrumpCards.Count)
            {
                return myTrumpCards.OrderBy(c => c.GetValue())
                    .FirstOrDefault(c => c.GetValue() < 10);
            }

            return null;
        }

        private Card GetPossibleSureCardsWhenGameClosed()
        {
            foreach (var myCard in this.possibleCardsToPlay)
            {
                if (myCard.Type == CardType.Ace)
                {
                    if (this.cardTracker.CountPlayedCardsInSuit(myCard.Suit) < 3)
                    {
                        return myCard;
                    }
                }
                else if (myCard.Type == CardType.Ten)
                {
                    if (this.cardTracker.FindPlayedCard(CardType.Ace, myCard.Suit) != null 
                        || this.cardValidator.HasHigherCard(myCard, this.possibleCardsToPlay))
                    {
                        if (this.cardTracker.CountPlayedCardsInSuit(myCard.Suit) < 3)
                        {
                            return myCard;
                        }
                    }
                }
            }

            return null;
        }
    }
}
