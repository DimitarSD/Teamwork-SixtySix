namespace Santase.AI.NinjaPlayer.ChooseCardStrategies
{
    using System.Collections.Generic;
    using System.Linq;

    using Santase.AI.NinjaPlayer.ChooseCardStrategies.Contracts;
    using Santase.Logic.Players;
    using Santase.Logic.Cards;
    using Helpers;

    public class ChooseCardWhenFirstAndGameNotClosed : BaseChooseCardStrategy, IChooseCardStrategy
    {
        public ChooseCardWhenFirstAndGameNotClosed(ICollection<Card> possibleCardsToPlay, CardTracker cardTracker, CardValidator cardValidator)
            :base (possibleCardsToPlay, cardTracker, cardValidator)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> cards)
        {
            Card card;
            var announce = AnnounceMarriage(context, cards);
            if (announce != null)
            {
                return announce;
            }

            if (this.cardTracker.MyTrickPoints >= 40
                && this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ace)
                && this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ten))
            {
                card = this.possibleCardsToPlay
                    .OrderByDescending(c => c.GetValue())
                    .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);
            }

            if (this.cardTracker.MyTrickPoints >= 50 && (this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ace)))
            {
                card = this.possibleCardsToPlay
                    .OrderByDescending(c => c.GetValue())
                    .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);
            }

            var shortestOpponentSuit = this.cardTracker.RemainingCards
                .GroupBy(x => x.Suit)
                .OrderBy(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(s => s != this.cardTracker.TrumpSuit);

            card = this.possibleCardsToPlay.OrderBy(x => x.GetValue()).FirstOrDefault(c => c.Suit == shortestOpponentSuit);

            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            card = this.GetSmallestNonTrumpCard();

            if (card == null)
            {
                // play smallest card
                card = this.GetSmallestCard();
            }

            return this.PlayCard(cards, card);
        }
    }
}
