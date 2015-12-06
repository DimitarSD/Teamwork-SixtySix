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
            :base (possibleCardsToPlay, cardTracker, cardValidator)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> cards)
        {
            Card card;
            // if firstPlayed card is not trump and we have a higher card of the same suit => play highest card
            if (!this.cardValidator.IsTrump(context.FirstPlayedCard, this.cardTracker.TrumpSuit))
            {
                // TODO: when 1 card left in deck - let him win?
                if (this.cardValidator.HasHigherCard(context.FirstPlayedCard, this.possibleCardsToPlay))
                {
                    // play higher card
                    card = possibleCardsToPlay
                        .Select(c => this.GetHigherCard(context.FirstPlayedCard)).FirstOrDefault();

                    if (!this.cardValidator.IsCardInAnnounce(context, card, this.possibleCardsToPlay, Announce.Twenty))
                    {
                        return this.PlayCard(cards, card);
                    }
                }
                else
                {
                    if (this.cardValidator.HasAnnounce(context, Announce.Twenty, cards) && this.cardTracker.MyTrickPoints + context.FirstPlayedCard.GetValue() >= 40)
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null)
                        {
                            return this.PlayCard(cards, card);
                        }
                    }

                    if (this.cardValidator.HasAnnounce(context, Announce.Twenty, cards) && this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() >= 50)
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null)
                        {
                            return this.PlayCard(cards, card);
                        }
                    }

                    if (this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() >= 50)
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null)
                        {
                            return this.PlayCard(cards, card);
                        }
                    }

                    if (this.cardValidator.IsTenOrAce(context.FirstPlayedCard) && this.cardValidator.HasTrumpCard(context, this.possibleCardsToPlay))
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (this.cardTracker.MyTrickPoints >= 66 - (context.FirstPlayedCard.GetValue() + card.GetValue()))
                        {
                            return this.PlayCard(cards, card);
                        }

                        // play smallest trump
                        card = possibleCardsToPlay
                            .OrderBy(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Jack);

                        if (card != null)
                        {
                            return this.PlayCard(cards, card);
                        }

                        card = possibleCardsToPlay
                            .OrderBy(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && context.TrumpCard.Type == CardType.Jack && c.Type == CardType.Nine);

                        if (card != null)
                        {
                            return this.PlayCard(cards, card);
                        }

                        card = possibleCardsToPlay
                            .OrderBy(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && (c.Type == CardType.Queen
                            && this.cardTracker.FindPlayedCard(CardType.King, this.cardTracker.TrumpSuit) != null)
                            || (c.Type == CardType.King
                            && this.cardTracker.FindPlayedCard(CardType.Queen, this.cardTracker.TrumpSuit) != null));

                        if (card != null)
                        {
                            return this.PlayCard(cards, card);
                        }

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
                    else
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null && this.cardTracker.MyTrickPoints >= 66 - (context.FirstPlayedCard.GetValue() + card.GetValue()))
                        {
                            return this.PlayCard(cards, card);
                        }

                        // play smallest card
                        card = this.GetSmallestCard();
                        return this.PlayCard(cards, card);
                    }
                }
            }
            else
            {
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
                    // try to win
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

                    card = possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && context.TrumpCard.Type == CardType.Jack && c.Type == CardType.Nine);

                    if (card != null)
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

            card = this.GetSmallestCard();
            return this.PlayCard(cards, card);
        }
    }
}
