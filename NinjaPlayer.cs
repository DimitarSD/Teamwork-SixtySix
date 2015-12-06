﻿namespace Santase.AI.NinjaPlayer
{
    using Santase.AI.NinjaPlayer.Helpers;
    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using ChooseCardStrategies;
    using ChooseCardStrategies.Contracts;

    public class NinjaPlayer : BasePlayer
    {
        private readonly static Dictionary<string, int> actionTypes = new Dictionary<string, int>()
            {
                { "ChooseCardWhenFirstAndGameNotClosed", 0 },
                { "ChooseCardWhenSecondAndGameNotClosed", 0 },
                { "ChooseCardWhenFirstAndGameClosed", 0 },
                { "ChooseCardWhenSecondAndGameClosed", 0 }
            };

        private readonly CardTracker cardTracker;
        private readonly CardValidator cardValidator;
        private ICollection<Card> possibleCardsToPlay;

        public NinjaPlayer()
            : this("Ninj66s")
        {
        }

        public NinjaPlayer(string name)
            : this(name, new CardValidator(), new CardTracker())
        {
        }

        public NinjaPlayer(string name, CardValidator cardValidator, CardTracker cardTracker)
        {
            this.Name = name;
            this.cardTracker = cardTracker;
            this.cardValidator = cardValidator;
            this.possibleCardsToPlay = new List<Card>();
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

            var strategy = this.GetStrategy(context);
            return strategy.ChooseCard(context, this.Cards);
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
            this.cardTracker.ClearMySureCards();
            this.cardTracker.AddPlayedCard(context.FirstPlayedCard);
            this.cardTracker.AddPlayedCard(context.SecondPlayedCard);
        }

        private bool ShouldCloseGame(PlayerTurnContext context)
        {
            this.cardTracker.GetSureCardsWhenGameClosed(context, this.possibleCardsToPlay);

            bool isCloseGameValid = this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards);
            bool hasFiveTrumpCards = this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 5;
            bool hasFourTrumpCards = this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 4;
            bool has40AndEnoughPoints = this.cardValidator.HasAnnounce(context, Announce.Forty, this.Cards)
                && (this.cardTracker.MyTrickPoints >= 26
                || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace) || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ten)
                || this.cardValidator.HasAnnounce(context, Announce.Twenty, this.Cards)
                || this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace) || this.cardValidator.HasNonTrumpCardType(context, this.Cards, CardType.Ace));
            bool hasTwentyAndEnoughPoints = this.cardValidator.HasAnnounce(context, Announce.Twenty, this.Cards)
                && (this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace) && this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ten)
                || hasFourTrumpCards
                || this.cardTracker.MyTrickPoints >= 20 && this.cardValidator.HasTrumpCardType(context, this.Cards, CardType.Ace)
                || this.cardTracker.MySureCards.Count > 2);

            //TODO: add more logic

            bool shouldCloseGame = isCloseGameValid && (hasFiveTrumpCards || has40AndEnoughPoints || hasTwentyAndEnoughPoints);

            return shouldCloseGame;
        }

        private IChooseCardStrategy GetStrategy(PlayerTurnContext context)
        {
            // game not closed
            if (!context.State.ShouldObserveRules)
            {
                if (context.IsFirstPlayerTurn)
                {
                    actionTypes["ChooseCardWhenFirstAndGameNotClosed"]++;
                    return new ChooseCardWhenFirstAndGameNotClosed(this.possibleCardsToPlay, this.cardTracker, this.cardValidator);
                }
                else
                {
                    actionTypes["ChooseCardWhenSecondAndGameNotClosed"]++;
                    return new ChooseCardWhenSecondAndGameNotClosed(this.possibleCardsToPlay, this.cardTracker, this.cardValidator);
                }
            }
            else
            {
                // game closed
                if (context.IsFirstPlayerTurn)
                {
                    actionTypes["ChooseCardWhenFirstAndGameClosed"]++;
                    return new ChooseCardWhenFirstAndGameClosed(this.possibleCardsToPlay, this.cardTracker, this.cardValidator);
                }
                else
                {
                    actionTypes["ChooseCardWhenSecondAndGameClosed"]++;
                    return new ChooseCardWhenSecondAndGameClosed(this.possibleCardsToPlay, this.cardTracker, this.cardValidator);
                }
            }
        }
    }
}
