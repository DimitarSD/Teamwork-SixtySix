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
        private readonly static Dictionary<string, int> actionTypes = new Dictionary<string, int>()
            {
                { "ChooseCardWhenFirstAndGameNotClosed", 0 },
                { "ChooseCardWhenSecondAndGameNotClosed", 0 },
                { "ChooseCardWhenFirstAndGameClosed", 0 },
                { "ChooseCardWhenSecondAndGameClosed", 0 }
            };
        //private readonly static Random random = new Random();

        // private readonly ICollection<Card> playedCards = new List<Card>();

        private readonly CardTracker cardTracker;
        private readonly CardValidator cardValidator;
        private IList<Card> possibleCardsToPlay;

        public NinjaPlayer()
            : this("Ninj66s")
        {
        }

        public NinjaPlayer(string name)
        {
            this.Name = name;
            this.possibleCardsToPlay = new List<Card>();
            this.cardValidator = new CardValidator(this.AnnounceValidator);
            this.cardTracker = new CardTracker();
        }

        public Dictionary<string, int> Tracker
        {
            get
            {
                return actionTypes;
            }
        }

        public override string Name { get; }

        // TODO: choose different strategy depending on game state
        public override PlayerAction GetTurn(PlayerTurnContext context)
        {
            this.possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards).ToList();
            this.cardTracker.GetRemainingCards(this.Cards, context.CardsLeftInDeck == 0 ? null : context.TrumpCard);
            this.cardTracker.GetMyRemainingTrumpCards(this.possibleCardsToPlay);
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

        public override void StartRound(ICollection<Card> cards, Card trumpCard, int myTotalPoints, int opponentTotalPoints)
        {
            this.cardTracker.TrumpSuit = trumpCard.Suit;
            this.cardTracker.GetFullDeck();
            base.StartRound(cards, trumpCard, myTotalPoints, opponentTotalPoints);
        }

        public override void EndRound()
        {
            this.cardTracker.ClearPlayedCards();
            this.cardTracker.ClearRemainingCards();
            base.EndRound();
        }

        public override void EndTurn(PlayerTurnContext context)
        {
            this.cardTracker.ClearMyRemainingTrumpCards();
            this.cardTracker.AddPlayedCard(context.FirstPlayedCard);
            this.cardTracker.AddPlayedCard(context.SecondPlayedCard);
        }

        private bool ShouldCloseGame(PlayerTurnContext context)
        {
            bool isCloseGameValid = this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards);
            bool hasFiveTrumpCards = this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 5;
            bool hasFourTrumpCards = this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 4;
            bool has40AndEnoughPoints = this.GetAnnounce(context, Announce.Forty) != null
                && (this.cardTracker.MyTrickPoints >= 26
                || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace) || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ten)
                || this.GetAnnounce(context, Announce.Twenty) != null
                || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace) || this.cardValidator.HasNonTrumpCardType(context, this.Cards, CardType.Ace));
            bool hasTwentyAndEnoughPoints = this.GetAnnounce(context, Announce.Twenty) != null
                && (this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace) && this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ten)
                || hasFourTrumpCards
                || this.cardTracker.MyTrickPoints >= 20 && this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace)
                || this.GetSureCardsWhenGameClosed(context).Count > 2);

            //TODO: add more logic

            bool shouldCloseGame = isCloseGameValid && (hasFiveTrumpCards || has40AndEnoughPoints || hasTwentyAndEnoughPoints);

            return shouldCloseGame;
        }

        private PlayerAction ChooseCard(PlayerTurnContext context)
        {
            // game not closed
            if (!context.State.ShouldObserveRules)
            {
                if (context.IsFirstPlayerTurn)
                {
                    actionTypes["ChooseCardWhenFirstAndGameNotClosed"]++;
                    return this.ChooseCardWhenFirstAndGameNotClosed(context);
                }
                else
                {
                    actionTypes["ChooseCardWhenSecondAndGameNotClosed"]++;
                    return this.ChooseCardWhenSecondAndGameNotClosed(context);
                }
            }
            else
            {
                // game closed
                if (context.IsFirstPlayerTurn)
                {
                    actionTypes["ChooseCardWhenFirstAndGameClosed"]++;
                    return this.ChooseCardWhenFirstAndGameClosed(context);
                }
                else
                {
                    actionTypes["ChooseCardWhenSecondAndGameClosed"]++;
                    return this.ChooseCardWhenSecondAndGameClosed(context);
                }
            }
        }

        private PlayerAction ChooseCardWhenFirstAndGameNotClosed(PlayerTurnContext context)
        {
            Card card;
            var announce = AnnounceMarriage(context);
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
                return this.PlayCard(card);
            }

            card = this.GetSmallestNonTrumpCard();

            if (card == null)
            {
                // play smallest card
                card = this.GetSmallestCard();
            }

            return this.PlayCard(card);
        }

        private PlayerAction ChooseCardWhenSecondAndGameNotClosed(PlayerTurnContext context)
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

                    if (!this.cardValidator.HasAnnounce(context, card, this.possibleCardsToPlay, Announce.Twenty))
                    {
                        return this.PlayCard(card);
                    }
                }
                else
                {
                    if (this.GetAnnounce(context, Announce.Twenty) != null && this.cardTracker.MyTrickPoints + context.FirstPlayedCard.GetValue() >= 40)
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }
                    }

                    if (this.GetAnnounce(context, Announce.Twenty) != null && this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() >= 50)
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }
                    }

                    if (this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() >= 50)
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }
                    }

                    if (this.cardValidator.IsTenOrAce(context.FirstPlayedCard) && this.cardValidator.HasTrumpCard(context, this.possibleCardsToPlay))
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (this.cardTracker.MyTrickPoints >= 66 - (context.FirstPlayedCard.GetValue() + card.GetValue()))
                        {
                            return this.PlayCard(card);
                        }

                        // play smallest trump
                        card = possibleCardsToPlay
                            .OrderBy(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Jack);

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }

                        card = possibleCardsToPlay
                            .OrderBy(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && context.TrumpCard.Type == CardType.Jack && c.Type == CardType.Nine);

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }

                        card = possibleCardsToPlay
                            .OrderBy(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && (c.Type == CardType.Queen
                            && this.cardTracker.FindPlayedCard(CardType.King, this.cardTracker.TrumpSuit) != null)
                            || (c.Type == CardType.King
                            && this.cardTracker.FindPlayedCard(CardType.Queen, this.cardTracker.TrumpSuit) != null));

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }

                        card = possibleCardsToPlay
                            .OrderBy(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.GetValue() >= 10);

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }

                        card = this.GetSmallestTrumpCard();

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }
                    }
                    else
                    {
                        card = possibleCardsToPlay
                            .OrderByDescending(c => c.GetValue())
                            .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null && this.cardTracker.MyTrickPoints >= 66 - (context.FirstPlayedCard.GetValue() + card.GetValue()))
                        {
                            return this.PlayCard(card);
                        }

                        // play smallest card
                        card = this.GetSmallestCard();
                        return this.PlayCard(card);
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
                        return this.PlayCard(card);
                    }
                }

                if (context.CardsLeftInDeck == 2)
                {
                    // try to win
                    card = this.possibleCardsToPlay.OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.GetValue() > context.FirstPlayedCard.GetValue() 
                        && !this.cardValidator.HasAnnounce(context, c, this.possibleCardsToPlay, Announce.Forty));

                    if (card != null)
                    {
                        return this.PlayCard(card);
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
                        return this.PlayCard(card);
                    }

                    card = possibleCardsToPlay
                        .OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && context.TrumpCard.Type == CardType.Jack && c.Type == CardType.Nine);

                    if (card != null)
                    {
                        return this.PlayCard(card);
                    }
                }
            }

            // play smallest non-trump card
            card = this.GetSmallestNonTrumpCard();
            if (card != null)
            {
                return this.PlayCard(card);
            }

            card = this.GetSmallestCard();
            return this.PlayCard(card);
        }

        private PlayerAction ChooseCardWhenFirstAndGameClosed(PlayerTurnContext context)
        {
            Card card;
            if (context.CardsLeftInDeck == 0)
            {
                return this.ChooseCardWhenFirstAndNoCardsInDeck(context);
            }
            else
            {
                var sureCards = this.GetSureCardsWhenGameClosed(context);

                if (sureCards.Count > 0)
                {
                    card = sureCards.First();
                    return this.PlayCard(card);
                }

                // play trump ace
                if (this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ace))
                {
                    card = this.possibleCardsToPlay.FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Ace);
                    return this.PlayCard(card);
                }

                // play trump 10 if ace is played
                if (this.cardValidator.HasTrumpCardType(context, this.possibleCardsToPlay, CardType.Ten)
                    && this.cardTracker.FindPlayedCard(CardType.Ace, this.cardTracker.TrumpSuit) != null)
                {
                    card = this.possibleCardsToPlay.FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit && c.Type == CardType.Ten);
                    return this.PlayCard(card);
                }

                // announce marriage
                var announce = AnnounceMarriage(context);
                if (announce != null)
                {
                    return announce;
                }

                card = this.GetPossibleSureCardsWhenGameClosed();

                if (card != null)
                {
                    return this.PlayCard(card);
                }

                card = this.ControlTrumpWhenGameClosed(this.cardTracker.TrumpSuit);

                if (card != null)
                {
                    return this.PlayCard(card);
                }

                card = this.GetSmallestNonTrumpCard();

                if (card != null)
                {
                    return this.PlayCard(card);
                }

                card = this.GetSmallestCard();

                return this.PlayCard(card);
            }
        }

        private PlayerAction ChooseCardWhenSecondAndGameClosed(PlayerTurnContext context)
        {
            Card card;
            if (!this.cardValidator.IsTrump(context.FirstPlayedCard, this.cardTracker.TrumpSuit))
            {
                card = this.GetHigherCard(context.FirstPlayedCard);

                if (card != null && !this.cardValidator.HasAnnounce(context, card, this.possibleCardsToPlay, Announce.Twenty))
                {
                    return this.PlayCard(card);
                }
            }
            else
            {
                if (context.CardsLeftInDeck == 0)
                {
                    // opponent has Ace and plays small trump => play trump 10
                    if (context.FirstPlayedCard.Type != CardType.Ace && this.cardTracker.FindRemainingCard(CardType.Ace, this.cardTracker.TrumpSuit) != null)
                    {
                        card = this.possibleCardsToPlay.FirstOrDefault(c => c.Type == CardType.Ten && c.Suit == this.cardTracker.TrumpSuit);

                        if (card != null)
                        {
                            return this.PlayCard(card);
                        }
                    }
                }


                // player has Ace and first played card is not Ten => play smallest trump to win
                if (this.cardTracker.FindMyRemainingTrumpCard(CardType.Ace) != null && context.FirstPlayedCard.Type != CardType.Ten)
                {
                    // if it will win the round => play Ace
                    if (this.cardTracker.MyTrickPoints + context.FirstPlayedCard.GetValue() >= 55 || context.FirstPlayerAnnounce == Announce.Forty)
                    {
                        card = this.possibleCardsToPlay.FirstOrDefault(c => c.Type == CardType.Ace);
                        return this.PlayCard(card);
                    }

                    // play smallest trump that will win
                    card = this.possibleCardsToPlay.OrderBy(c => c.GetValue())
                        .FirstOrDefault(c => c.GetValue() > context.FirstPlayedCard.GetValue()
                        && !this.cardValidator.HasAnnounce(context, c, this.possibleCardsToPlay, Announce.Forty));

                    if (card != null)
                    {
                        return this.PlayCard(card);
                    }

                    // play smallest trum that will not win
                    card = this.possibleCardsToPlay.OrderBy(c => c.GetValue())
                        .FirstOrDefault();

                    if (this.cardTracker.OpponentsTrickPoints + context.FirstPlayedCard.GetValue() + card.GetValue() < 55)
                    {
                        return this.PlayCard(card);
                    }

                    // play trump Ace
                    card = this.possibleCardsToPlay.FirstOrDefault(c => c.Type == CardType.Ace);
                    return this.PlayCard(card);
                }

                card = this.GetHigherCard(context.FirstPlayedCard);

                if (card != null && !this.cardValidator.HasAnnounce(context, card, this.possibleCardsToPlay, Announce.Forty))
                {
                    return this.PlayCard(card);
                }
            }

            // play smallest non-trump card
            card = this.GetSmallestNonTrumpCard();

            if (card != null)
            {
                return this.PlayCard(card);
            }

            card = GetSmallestNonAnnounceTrumpCard(context);
            if (card != null)
            {
                return this.PlayCard(card);
            }

            card = this.GetSmallestCard();
            return this.PlayCard(card);
        }

        private PlayerAction ChooseCardWhenFirstAndNoCardsInDeck(PlayerTurnContext context)
        {
            Card card;
            var sureCards = this.GetSureCardsWhenGameClosed(context, false);

            if (sureCards.Count > 0)
            {
                card = sureCards.First();
                return this.PlayCard(card);
            }

            var announce = AnnounceMarriage(context);
            if (announce != null)
            {
                return announce;
            }

            card = ControlTrumpWhenGameClosed(this.cardTracker.TrumpSuit);

            if (card != null)
            {
                return this.PlayCard(card);
            }

            card = this.GetSmallestNonTrumpCard();

            if (card != null)
            {
                return this.PlayCard(card);
            }

            return this.PlayCard(this.GetSmallestCard());
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
                if (this.cardValidator.HasAnnounce(context, card, this.Cards, announce))
                {
                    return card;
                }
            }

            return null;
        }

        public ICollection<Card> GetSureCardsWhenGameClosed(PlayerTurnContext context, bool deckHasCards = true)
        {
            var sureCards = new List<Card>();

            foreach (var myCard in possibleCardsToPlay)
            {
                var opponentsCardsInSuit = this.GetOpponentsCardInSuit(myCard.Suit);

                if (this.cardValidator.HasAnnounce(context, myCard, this.Cards, Announce.Forty)
                    || this.cardValidator.HasAnnounce(context, myCard, this.Cards, Announce.Twenty))
                {
                    continue;
                }

                if (!this.cardValidator.HasTrumpCard(context, this.cardTracker.RemainingCards)
                    && opponentsCardsInSuit.Count == 0 && myCard.Suit != this.cardTracker.TrumpSuit)
                {
                    sureCards.Add(myCard);
                }

                foreach (var opponetCard in opponentsCardsInSuit)
                {
                    if (myCard.GetValue() < opponetCard.GetValue())
                    {
                        break;
                    }
                    else
                    {
                        if (deckHasCards)
                        {
                            if (!this.cardValidator.HasTrumpCard(context, this.cardTracker.RemainingCards))
                            {
                                this.cardTracker.MySureTrickPoints += (myCard.GetValue() + opponetCard.GetValue());
                                sureCards.Add(myCard);
                            }
                        }
                        else
                        {
                            this.cardTracker.MySureTrickPoints += (myCard.GetValue() + opponetCard.GetValue());
                            sureCards.Add(myCard);
                        }

                        break;
                    }
                }
            }

            return sureCards;
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


        private ICollection<Card> GetOpponentsCardInSuit(CardSuit suit)
        {
            return this.cardTracker.RemainingCards.Where(c => c.Suit == suit)
                .OrderByDescending(c => c.GetValue()).ToList();
        }

        private Card GetSmallestNonTrumpCard()
        {
            return possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit != this.cardTracker.TrumpSuit);
        }

        private Card GetSmallestNonAnnounceNonTrumpCard(PlayerTurnContext context)
        {
            return possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit != this.cardTracker.TrumpSuit 
                && !this.cardValidator.HasAnnounce(context, c, this.possibleCardsToPlay, Announce.Twenty));
        }

        private Card GetSmallestTrumpCard()
        {
            return this.possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit);
        }

        private Card GetSmallestNonAnnounceTrumpCard(PlayerTurnContext context)
        {
            return this.possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == this.cardTracker.TrumpSuit
                && !this.cardValidator.HasAnnounce(context, c, this.possibleCardsToPlay, Announce.Forty));
        }

        private Card GetSmallestCard()
        {
            return this.possibleCardsToPlay
                .OrderBy(c => c.GetValue())
                .FirstOrDefault();
        }

        private Card GetHigherCard(Card firstPlayedCard)
        {
            return this.possibleCardsToPlay.OrderByDescending(c => c.GetValue())
                .FirstOrDefault(c => c.Suit == firstPlayedCard.Suit && c.GetValue() > firstPlayedCard.GetValue());
        }
    }
}
