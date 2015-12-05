namespace Santase.AI.NinjaPlayer
{
    using Santase.AI.NinjaPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    public class NinjaPlayer : BasePlayer
    {
        //private readonly static Random random = new Random();

        // private readonly ICollection<Card> playedCards = new List<Card>();

        private readonly CardTracker cardTracker;
        private readonly CardValidator cardValidator;
        private ICollection<Card> possibleCardsToPlay;

        public NinjaPlayer(string name = "Pesho")
        {
            this.Name = name;
            this.possibleCardsToPlay = new List<Card>();
            this.cardValidator = new CardValidator();
            this.cardTracker = new CardTracker();
        }

        public override string Name { get; }

        // TODO: choose different strategy depending on game state
        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);
            this.cardTracker.GetRemainingCards(this.Cards, context.CardsLeftInDeck == 0 ? null : context.TrumpCard);
            this.cardTracker.GetTrickPoints(context);

            if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard);
            }

            if (this.ShouldCloseGame(context))
            {
                return this.CloseGame();
            }

            return this.ChooseCard(context);
        }

        public override void EndRound()
        {
            this.cardTracker.ClearPlayedCards();
            base.EndRound();
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.cardTracker.AddPlayedCard(context.FirstPlayedCard);
            this.cardTracker.AddPlayedCard(context.SecondPlayedCard);
        }

        private bool ShouldCloseGame(PlayerTurnContext context)
        {
            bool isCloseGameValid = this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards);
            bool hasFiveTrumpCards = this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 5;
            bool has40AndEnoughPoints = this.GetAnnounce(context, Announce.Forty) != null
                && (this.cardTracker.MyTrickPoints >= 26
                || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace) || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ten)
                || this.GetAnnounce(context, Announce.Twenty) != null
                || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace) || this.cardValidator.HasNonTrumpCardType(context, this.Cards, CardType.Ace));

            //TODO: add more logic

            bool shouldCloseGame = isCloseGameValid && (hasFiveTrumpCards || has40AndEnoughPoints);

            return shouldCloseGame;
        }

        // TODO: count cards
        // TODO: count points
        private PlayerAction ChooseCard(PlayerTurnContext context)
        {
            // game not closed
            if (!context.State.ShouldObserveRules)
            {
                if (context.IsFirstPlayerTurn)
                {
                    return this.ChooseCardWhenFirstAndGameNotClosed(context);
                }
                else
                {
                    return this.ChooseCardWhenSecondAndGameNotClosed(context);
                }
            }
            else
            {
                // game closed
                if (context.IsFirstPlayerTurn)
                {
                    return this.ChooseCardWhenFirstAndGameClosed(context);
                }
                else
                {
                    return this.ChooseCardWhenSecondAndGameClosed(context);
                }
            }
        }

        private PlayerAction ChooseCardWhenFirstAndGameNotClosed(PlayerTurnContext context)
        {
            var announce = AnnounceMarriage(context);
            if (announce != null)
            {
                return announce;
            }

            // play smallest non-trump card
            var card = this.GetSmallestNonTrumpCard(context);

            if (card == null)
            {
                // play smallest card
                card = this.GetSmallestCard();
            }

            return this.PlayCard(card);
        }

        private PlayerAction ChooseCardWhenSecondAndGameNotClosed(PlayerTurnContext context)
        {
            // TODO: check for announce when playing card

            Card card;
            // if firstPlayed card is not trump and we have a higher card of the same suit => play highest card
            if (!this.cardValidator.IsTrump(context.FirstPlayedCard, context.TrumpCard.Type))
            {
                if (this.cardValidator.HasHigherCard(context.FirstPlayedCard, this.possibleCardsToPlay))
                {
                    // play higher card
                    card = possibleCardsToPlay
                        .Select(c => this.GetHigherCard(context.FirstPlayedCard)).FirstOrDefault();
                }
                else
                {
                    if (this.cardValidator.IsTenOrAce(context.FirstPlayedCard) && this.cardValidator.HasTrumpCard(context, this.possibleCardsToPlay))
                    {
                        // play smallest trump
                        card = this.GetSmallestTrumpCard(context);
                    }
                    else
                    {
                        // play smallest card
                        card = this.GetSmallestCard();
                    }
                }
            }
            else
            {
                // play smallest non-trump card
                card = this.GetSmallestNonTrumpCard(context);
            }

            return this.PlayCard(card);
        }

        private PlayerAction ChooseCardWhenFirstAndGameClosed(PlayerTurnContext context)
        {
            // TODO check for announce in sure cards
            Card card;
            if (context.CardsLeftInDeck == 0)
            {
                var sureCards = this.GetSureCardsWhenGameClosed(context);

                if (sureCards.Count > 0)
                {
                    card = sureCards.First();
                    return this.PlayCard(card);
                }
            }

            // play trump ace or ten                   
            card = possibleCardsToPlay
                .OrderByDescending(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == context.TrumpCard.Suit && this.cardValidator.IsTenOrAce(c));

            if (card != null)
            {
                return this.PlayCard(card);
            }

            var announce = AnnounceMarriage(context);
            if (announce != null)
            {
                return announce;
            }

            // TODO: better logic

            // play non-trump ace or ten
            card = possibleCardsToPlay
                .OrderByDescending(c => c.GetValue())
                .FirstOrDefault(c => c.Suit != context.TrumpCard.Suit && this.cardValidator.IsTenOrAce(c));

            if (card != null)
            {
                return this.PlayCard(card);
            }

            card = this.GetSmallestNonTrumpCard(context);

            if (card != null)
            {
                return this.PlayCard(card);
            }

            card = this.GetSmallestCard();

            return this.PlayCard(card);

            // todo check counted cards
        }

        private PlayerAction ChooseCardWhenSecondAndGameClosed(PlayerTurnContext context)
        {
            //TODO: check for announce when playing card
            // play smallest non-trump card
            var card = this.GetSmallestNonTrumpCard(context);

            if (card != null)
            {
                return this.PlayCard(card);
            }

            card = this.GetSmallestCard();
            return this.PlayCard(card);
        }

        private PlayerAction AnnounceMarriage(PlayerTurnContext context)
        {
            if (context.State.CanAnnounce20Or40)
            {
                // get 40
                var card = this.GetAnnounce(context, Announce.Forty);

                if (card != null)
                {
                    return this.PlayCard(card);
                }

                // get 20
                card = this.GetAnnounce(context, Announce.Twenty);

                if (card != null)
                {
                    return this.PlayCard(card);
                }
            }

            return null;
        }

        private Card GetAnnounce(PlayerTurnContext context, Announce announce)
        {
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard) == announce)
                {
                    return card;
                }
            }

            return null;
        }

        public ICollection<Card> GetSureCardsWhenGameClosed(PlayerTurnContext context)
        {
            var sureCards = new List<Card>();

            foreach (var myCard in possibleCardsToPlay)
            {
                foreach (var opponetCard in this.cardTracker.RemainingCards.OrderByDescending(c => c.GetValue()))
                {
                    if (myCard.Suit == opponetCard.Suit && myCard.GetValue() > opponetCard.GetValue())
                    {
                        this.cardTracker.MySureTrickPoints += (myCard.GetValue() + opponetCard.GetValue());
                        sureCards.Add(myCard);
                    }
                }

                //if (!this.cardValidator.HasTrumpCard(context, this.cardTracker.RemainingCards)
                //    && !this.cardValidator.HasAnyCardInSuit(this.cardTracker.RemainingCards, myCard.Suit))
                //{
                //    sureCards.Add(myCard);
                //}
            }

            return sureCards;
        }

        private Card GetSmallestNonTrumpCard(PlayerTurnContext context)
        {
            return possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit != context.TrumpCard.Suit);
        }

        private Card GetSmallestTrumpCard(PlayerTurnContext context)
        {
            return possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == context.TrumpCard.Suit);
        }

        private Card GetSmallestCard()
        {
            return possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault();
        }

        private Card GetHigherCard(Card firstPlayedCard)
        {
            return possibleCardsToPlay.OrderByDescending(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == firstPlayedCard.Suit && c.GetValue() > firstPlayedCard.GetValue());
        }
    }
}
