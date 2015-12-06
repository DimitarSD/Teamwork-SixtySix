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
                // TODO: when 1 card left in deck - let him win?

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
                    || this.cardValidator.HasAnnounce(context, Announce.Forty, cards) && this.cardTracker.MyTrickPoints + context.FirstPlayedCard.GetValue() >= 10)
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

                //todo: check if it chould be done && check for possible 40
                if (context.CardsLeftInDeck == 2 && (context.TrumpCard.Type != CardType.Ace || context.TrumpCard.Type != CardType.Ten))
                {
                    // try to win so player is first when closed
                    card = this.possibleCardsToPlay.OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit
                        && !this.cardValidator.IsCardInAnnounce(context, c, this.possibleCardsToPlay, Announce.Forty));

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }
                }

                //// TODO: check if this should be done
                //// if player can have forty by changing trump
                //if (this.cardValidator.HasTrumpCardType(context, cards, CardType.Nine) && context.CardsLeftInDeck > 4
                //    && (context.TrumpCard.Type == CardType.Queen && this.cardValidator.HasTrumpCardType(context, cards, CardType.King)
                //        || (context.TrumpCard.Type == CardType.King && this.cardValidator.HasTrumpCardType(context, cards, CardType.Queen))))
                //{
                //    card = this.GetHighestCardInSuit(this.possibleCardsToPlay, this.cardTracker.TrumpSuit);
                //    if (card != null && card.Type != CardType.Queen && card.Type != CardType.King && card.Type != CardType.Nine)
                //    {
                //        return this.PlayCard(cards, card);
                //    }
                //}

                //// let opponent win of player can have forty by taking the last card in the deck
                //if (context.CardsLeftInDeck == 2 && this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() < 55
                //    && (context.TrumpCard.Type == CardType.Queen && this.cardValidator.HasTrumpCardType(context, cards, CardType.King)
                //        || (context.TrumpCard.Type == CardType.King && this.cardValidator.HasTrumpCardType(context, cards, CardType.Queen))
                //        || context.TrumpCard.Type == CardType.Ace || context.TrumpCard.Type == CardType.Ten))
                //{
                //    card = this.GetSmallestNonAnnounceNonTrumpCard(context);
                //    if (card != null)
                //    {
                //        return this.PlayCard(cards, card);
                //    }

                //    // should never go here
                //    card = this.GetSmallestNonTrumpCard();
                //    if (card != null)
                //    {
                //        return this.PlayCard(cards, card);
                //    }
                //}

                // if oppponent plays ace or ten
                if (this.cardValidator.IsTenOrAce(context.FirstPlayedCard))
                {
                    // play higher Ace
                    card = this.GetHigherCard(context.FirstPlayedCard);
                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }

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
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Nine
                        && (context.TrumpCard.Type == CardType.Jack
                        || (context.CardsLeftInDeck == 2 && context.TrumpCard.GetValue() < 10
                        && ((context.TrumpCard.Type == CardType.Queen && this.cardValidator.HasTrumpCardType(context, cards, CardType.King)
                        || (context.TrumpCard.Type == CardType.King && this.cardValidator.HasTrumpCardType(context, cards, CardType.Queen)))))));

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }

                    // play King or Queen if the other is already played - no chance for forty
                    card = possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit
                        && (c.Type == CardType.Queen && this.cardTracker.FindPlayedCard(CardType.King, this.cardTracker.TrumpSuit) != null)
                        || (c.Type == CardType.King && this.cardTracker.FindPlayedCard(CardType.Queen, this.cardTracker.TrumpSuit) != null));

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
                //else
                //{
                //    card = this.GetHighestCardInSuit(this.possibleCardsToPlay, this.cardTracker.TrumpSuit);
                //    if (card != null && this.cardTracker.MyTrickPoints >= 66 - (context.FirstPlayedCard.GetValue() + card.GetValue()))
                //    {
                //        return this.PlayCard(cards, card);
                //    }

                //    // play smallest card
                //    card = this.GetSmallestCard();
                //    return this.PlayCard(cards, card);
                //}

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

                    //todo check trumpcard left
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

                    //card = possibleCardsToPlay
                    //    .OrderBy(c => c.GetValue())
                    //    .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && (context.TrumpCard.Type == CardType.Jack || c.Type == CardType.Nine));

                    //if (card != null)
                    //{
                    //    return this.PlayCard(cards, card);
                    //}
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
