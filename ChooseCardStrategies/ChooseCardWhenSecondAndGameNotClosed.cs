namespace Santase.AI.NinjaPlayer.ChooseCardStrategies
{
    using Santase.AI.NinjaPlayer.ChooseCardStrategies.Contracts;
    using System.Collections.Generic;
    using System.Linq;
    using Santase.Logic.Players;
    using Logic.Cards;
    using Logic;
    using Helpers;

    public class ChooseCardWhenSecondAndGameNotClosed : BaseChooseCardStrategy, IChooseCardStrategy
    {
        public ChooseCardWhenSecondAndGameNotClosed(ICollection<Card> possibleCardsToPlay, CardTracker cardTracker, CardValidator cardValidator)
            : base(possibleCardsToPlay, cardTracker, cardValidator)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> cards)
        {
            Card card;
            // if firstPlayed card is not trump and we have a higher card of the same suit => play highest card
            if (!this.cardValidator.IsTrump(context.FirstPlayedCard, this.cardTracker.TrumpSuit))
            {
                var asd = this.cardTracker.RemainingCards;
                var dsa = this.possibleCardsToPlay;

                // play higher card
                card = this.GetHigherCard(context.FirstPlayedCard);
                if (card != null && !this.cardValidator.IsCardInAnnounce(context, card, this.possibleCardsToPlay, Announce.Twenty))
                {
                    return this.PlayCard(cards, card);
                }

                // play trump to win the round
                card = this.GetHighestCardInSuit(this.possibleCardsToPlay, this.cardTracker.TrumpSuit);
                if (card != null && this.cardTracker.MyTrickPoints >= 66 - (context.FirstPlayedCard.GetValue() + card.GetValue()))
                {
                    return this.PlayCard(cards, card);
                }

                // if player has announce and enough points => play trump
                if ((this.cardValidator.HasAnnounce(context, Announce.Twenty, cards) && this.cardTracker.MyTrickPoints + context.FirstPlayedCard.GetValue() >= 40)
                    || (this.cardValidator.HasAnnounce(context, Announce.Twenty, cards) && this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() >= 50)
                    || this.cardValidator.HasAnnounce(context, Announce.Forty, cards) && (this.cardTracker.MyTrickPoints + context.FirstPlayedCard.GetValue() >= 10 || this.cardTracker.MyRemainingTrumpCards.Count >= 3))
                {
                    card = this.possibleCardsToPlay.OrderBy(c => c.GetValue()).FirstOrDefault(c => c.Type != CardType.Ace);
                    if (card != null && !this.cardValidator.IsCardInAnnounce(context, card, this.possibleCardsToPlay, Announce.Forty))
                    {
                        return this.PlayCard(cards, card);
                    }

                    card = this.GetHighestCardInSuit(this.possibleCardsToPlay, this.cardTracker.TrumpSuit);
                    if (card != null && !this.cardValidator.IsCardInAnnounce(context, card, this.possibleCardsToPlay, Announce.Forty))
                    {
                        return this.PlayCard(cards, card);
                    }
                }

                // if opponent is close to the win => play high trump to get more than 33
                if (this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() >= 50 && this.cardTracker.MyTrickPoints < 33)
                {
                    card = this.GetHighestCardInSuit(this.possibleCardsToPlay, this.cardTracker.TrumpSuit);
                    if (card != null && !this.cardValidator.IsCardInAnnounce(context, card, this.possibleCardsToPlay, Announce.Forty))
                    {
                        return this.PlayCard(cards, card);
                    }
                }

                // let opponent win of player can have forty by taking the last card in the deck
                if (!this.cardValidator.IsTenOrAce(context.FirstPlayedCard) && context.CardsLeftInDeck == 2 && this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() < 50 && this.cardTracker.MyTrickPoints < 50
                    && (this.cardTracker.MyRemainingTrumpCards.Count >= 3
                    && ((context.TrumpCard.Type == CardType.Queen && this.cardValidator.HasTrumpCardType(context, cards, CardType.King))
                        || (context.TrumpCard.Type == CardType.King && this.cardValidator.HasTrumpCardType(context, cards, CardType.Queen)))
                        || context.TrumpCard.Type == CardType.Ace))
                {
                    card = this.GetSmallestNonAnnounceNonTrumpCard(context);
                    if (card != null && card.GetValue() < 10)
                    {
                        return this.PlayCard(cards, card);
                    }
                }

                // if oppponent plays ace or ten
                if (this.cardValidator.IsTenOrAce(context.FirstPlayedCard))
                {
                    // play higher Ace
                    card = this.GetHigherCard(context.FirstPlayedCard);
                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }

                    // play high trump if it will win the trick
                    card = this.GetHighestCardInSuit(this.possibleCardsToPlay, this.cardTracker.TrumpSuit);
                    if (card != null && this.cardTracker.MyTrickPoints + context.FirstPlayedCard.GetValue() + card.GetValue() >= 66)
                    {
                        return this.PlayCard(cards, card);
                    }

                    // play Jack
                    card = this.possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Jack);

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }

                    // play 9 if Jack is trump or there are two cards left in deck
                    card = possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Nine);

                    if (card != null && (context.TrumpCard.Type == CardType.Jack || context.CardsLeftInDeck == 2))
                    {
                        return this.PlayCard(cards, card);
                    }

                    // play King or Queen if the other is already played - no chance for forty
                    card = possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit
                        && ((c.Type == CardType.Queen && this.cardTracker.FindPlayedCard(CardType.King, this.cardTracker.TrumpSuit) != null)
                        || (c.Type == CardType.King && this.cardTracker.FindPlayedCard(CardType.Queen, this.cardTracker.TrumpSuit) != null)));

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }

                    // play ten or ace to win
                    card = possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.GetValue() >= 10);

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }

                    card = this.GetSmallestTrumpCard();
                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }
                }
            }
            else
            {
                // if first played card is trump
                // if opponent has announced 40 or played ten => play higher
                if (context.FirstPlayerAnnounce == Announce.Forty || context.FirstPlayedCard.Type == CardType.Ten)
                {
                    card = this.GetHigherCard(context.FirstPlayedCard);
                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }
                }

                if (context.CardsLeftInDeck == 2)
                {
                    // try to win so player is first when closed
                    card = this.possibleCardsToPlay.OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.GetValue() > context.FirstPlayedCard.GetValue()
                        && !this.cardValidator.IsCardInAnnounce(context, c, this.possibleCardsToPlay, Announce.Forty));

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }
                }

                card = this.GetSmallestNonAnnounceNonTrumpCard(context);

                if (card.GetValue() >= 10)
                {
                    // play smallest trump
                    card = possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Jack);

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }

                    // play 9 if Jack is trump or there are two cards left in deck
                    card = possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Nine);

                    if (card != null && (context.TrumpCard.Type == CardType.Jack || context.CardsLeftInDeck == 2))
                    {
                        return this.PlayCard(cards, card);
                    }
                }
            }

            // play smallest non-trump card
            card = this.GetSmallestNonTrumpCard();
            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            return this.PlayCard(cards, this.GetSmallestCard());
        }
    }
}
