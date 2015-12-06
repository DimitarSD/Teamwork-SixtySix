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
            : base(possibleCardsToPlay, cardTracker, cardValidator)
        {
        }

        public override PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> cards)
        {
            // play announce
            var announce = AnnounceMarriage(context, cards);
            if (announce != null)
            {
                return announce;
            }

            Card card;

            // if player is pretty close to the win and has trump ace and ten => play them
            if (this.cardTracker.MyTrickPoints >= 40
                && this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ace)
                && this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ten))
            {
                card = this.possibleCardsToPlay
                    .OrderByDescending(c => c.GetValue())
                    .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                return this.PlayCard(cards, card);
            }

            // if player is close to win and has trump ace => play it
            if (this.cardTracker.MyTrickPoints >= 50 && this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ace))
            {
                card = this.possibleCardsToPlay
                    .OrderByDescending(c => c.GetValue())
                    .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                return this.PlayCard(cards, card);
            }

            // try to find suit from which opponent has no cards and play it
            var shortestOpponentSuit = this.GetOpponentsShortestSuit();
          
            card = this.possibleCardsToPlay.OrderBy(x => x.GetValue())
                .FirstOrDefault(c => c.Suit == shortestOpponentSuit);

            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            // play smallest non-trump
            card = this.GetSmallestNonTrumpCard();

            if (card != null)
            {
                return this.PlayCard(cards, card);
            }

            // play smallest card
            card = this.GetSmallestCard();
            return this.PlayCard(cards, card);
        }
    }
}
