namespace Santase.AI.NinjaPlayer.ChooseCardStrategies
{
    using Contracts;
    using System.Collections.Generic;
    using System.Linq;
    using Logic.Players;
    using Logic.Cards;
    using Logic;
    using Helpers;

    public class ChooseCardWhenSecondAndGameClosed : BaseChooseCardStrategy, IChooseCardStrategy
    {
        public ChooseCardWhenSecondAndGameClosed(ICollection<Card> possibleCardsToPlay, CardTracker cardTracker, CardValidator cardValidator)
            : base(possibleCardsToPlay, cardTracker, cardValidator)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> cards)
        {
            Card card;
            if (!this.cardValidator.IsTrump(context.FirstPlayedCard, this.cardTracker.TrumpSuit))
            {
                // play higher card not part of announce
                card = this.GetHigherCard(context.FirstPlayedCard);
                if (card != null && !this.cardValidator.IsCardInAnnounce(context, card, this.possibleCardsToPlay, Announce.Twenty))
                {
                    return this.PlayCard(cards, card);
                }

                // play high trump to win the round
                card = this.GetHighestCardInSuit(this.possibleCardsToPlay, this.cardTracker.TrumpSuit);
                if (card != null && this.cardTracker.MyTrickPoints >= 66 - (context.FirstPlayedCard.GetValue() + card.GetValue()))
                {
                    return this.PlayCard(cards, card);
                }
            }
            else
            {
                // opponent has played trump card
                //if (context.CardsLeftInDeck == 0)
                //{
                // opponent has Ace and plays small trump => play trump 10
                if (context.FirstPlayedCard.Type != CardType.Ace && this.cardTracker.FindRemainingCard(CardType.Ace, this.cardTracker.TrumpSuit) != null)
                {
                    card = this.possibleCardsToPlay.FirstOrDefault(c => c.Type == CardType.Ten && c.Suit == this.cardTracker.TrumpSuit);
                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }
                }

                // opponent plays forty => play card to win
                if (context.FirstPlayerAnnounce == Announce.Forty)
                {
                    card = this.GetHigherCard(context.FirstPlayedCard);

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }
                }
                //}

                // player has Ace and first played card is not Ten => play smallest trump to win
                if (this.cardTracker.FindMyRemainingTrumpCard(CardType.Ace) != null && context.FirstPlayedCard.Type != CardType.Ten)
                {
                    // if it will win the round or opponent has announced forty => play Ace
                    if (this.cardTracker.MyTrickPoints + context.FirstPlayedCard.GetValue() >= 55 || context.FirstPlayerAnnounce == Announce.Forty)
                    {
                        card = this.possibleCardsToPlay.FirstOrDefault(c => c.Type == CardType.Ace);
                        return this.PlayCard(cards, card);
                    }

                    // play smallest trump that will win and is not in forty
                    card = this.possibleCardsToPlay.OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.GetValue() > context.FirstPlayedCard.GetValue()
                        && !this.cardValidator.IsCardInAnnounce(context, c, this.possibleCardsToPlay, Announce.Forty));

                    if (card != null)
                    {
                        return this.PlayCard(cards, card);
                    }

                    // if opponent is not close to the win play smallest trump that will not win
                    card = this.possibleCardsToPlay.OrderBy(c => c.GetValue())
                        .FirstOrDefault();

                    if (this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() + card.GetValue() < 55)
                    {
                        return this.PlayCard(cards, card);
                    }

                    // play trump Ace
                    card = this.possibleCardsToPlay.FirstOrDefault(c => c.Type == CardType.Ace);
                    return this.PlayCard(cards, card);
                }

                card = this.GetHigherCard(context.FirstPlayedCard);
                if (card != null && !this.cardValidator.IsCardInAnnounce(context, card, this.possibleCardsToPlay, Announce.Forty))
                {
                    return this.PlayCard(cards, card);
                }
            }

            // play smallest non-announce non-trump card
            card = this.GetSmallestNonAnnounceNonTrumpCard(context);
            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            // play smallest non-trump card
            card = this.GetSmallestNonTrumpCard();
            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            card = GetSmallestNonAnnounceTrumpCard(context);
            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            return this.PlayCard(cards, this.GetSmallestCard());
        }
    }
}
