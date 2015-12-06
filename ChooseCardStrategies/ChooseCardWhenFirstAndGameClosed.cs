namespace Santase.AI.NinjaPlayer.ChooseCardStrategies
{
    using Santase.AI.NinjaPlayer.ChooseCardStrategies.Contracts;
    using System.Collections.Generic;
    using System.Linq;
    using Santase.Logic.Players;
    using Logic.Cards;
    using Logic;
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
            if (context.CardsLeftInDeck == 0)
            {
                return this.ChooseCardWhenFirstAndNoCardsInDeck(context, cards);
            }
            else
            {
                this.cardTracker.GetSureCardsWhenGameClosed(context, this.possibleCardsToPlay);
                var sureCards = this.cardTracker.MySureCards;

                if (sureCards.Count > 0)
                {
                    card = sureCards.First();
                    return this.PlayCard(cards, card);
                }

                // play trump ace
                if (this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ace))
                {
                    card = this.possibleCardsToPlay.FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Ace);
                    return this.PlayCard(cards, card);
                }

                // play trump 10 if ace is played
                if (this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ten)
                    && this.cardTracker.FindPlayedCard(CardType.Ace, this.cardTracker.TrumpSuit) != null)
                {
                    card = this.possibleCardsToPlay.FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Ten);
                    return this.PlayCard(cards, card);
                }

                // announce marriage
                var announce = AnnounceMarriage(context, cards);
                if (announce != null)
                {
                    return announce;
                }

                card = this.GetPossibleSureCardsWhenGameClosed();

                if (card != null)
                {
                    return this.PlayCard(cards, card);
                }

                card = this.ControlTrumpWhenGameClosed(this.cardTracker.TrumpSuit);

                if (card != null)
                {
                    return this.PlayCard(cards, card);
                }

                card = this.GetSmallestNonTrumpCard();

                if (card != null)
                {
                    return this.PlayCard(cards, card);
                }

                card = this.GetSmallestCard();

                return this.PlayCard(cards, card);
            }
        }

        private PlayerAction ChooseCardWhenFirstAndNoCardsInDeck(PlayerTurnContext context, ICollection<Card> cards)
        {
            Card card;
            this.cardTracker.GetSureCardsWhenGameClosed(context, this.possibleCardsToPlay, false);
            var sureCards = this.cardTracker.MySureCards;

            if (sureCards.Count > 0)
            {
                card = sureCards.First();
                return this.PlayCard(cards, card);
            }

            var announce = AnnounceMarriage(context, cards);
            if (announce != null)
            {
                return announce;
            }

            card = ControlTrumpWhenGameClosed(this.cardTracker.TrumpSuit);

            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            card = this.GetSmallestNonTrumpCard();

            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            return this.PlayCard(cards, this.GetSmallestCard());
        }

        private Card GetPossibleSureCardsWhenGameClosed()
        {
            foreach (var myCard in this.possibleCardsToPlay)
            {
                if (myCard.Type == CardType.Ace)
                {
                    if (this.cardTracker.CountPlayedCardsInSuit(myCard.Suit) == 0)
                    {
                        return myCard;
                    }

                    if (this.cardTracker.CountPlayedCardsInSuit(myCard.Suit) < 3)
                    {
                        return myCard;
                    }
                }
                else if (myCard.Type == CardType.Ten)
                {
                    if (this.cardTracker.FindPlayedCard(CardType.Ace, myCard.Suit) != null)
                    {
                        if (this.cardTracker.CountPlayedCardsInSuit(myCard.Suit) == 0)
                        {
                            return myCard;
                        }

                        if (this.cardTracker.CountPlayedCardsInSuit(myCard.Suit) < 3)
                        {
                            return myCard;
                        }
                    }
                }

            }

            return null;
        }

        private Card ControlTrumpWhenGameClosed(CardSuit trumpSuit)
        {
            var myTrumpCards = this.cardTracker.MyRemainingTrumpCards;
            var opponentsTrumpCards = this.cardTracker.RemainingCards.Where(c => c.Suit == trumpSuit).ToList();

            if (opponentsTrumpCards.Count == 0)
            {
                return null;
            }

            // play small non-trump card to make opponent play trump
            foreach (var myCard in this.possibleCardsToPlay.OrderBy(c => c.GetValue()))
            {
                if (this.cardValidator.HasAnyCardInSuit(this.cardTracker.RemainingCards, myCard.Suit) && myCard.Suit != trumpSuit && myCard.GetValue() < 10)
                {
                    return myCard;
                }
            }

            // play small trump card to make opponent play trump
            return myTrumpCards.OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);
        }
    }
}
