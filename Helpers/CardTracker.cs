﻿namespace Santase.AI.NinjaPlayer.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Logic.Cards;
    using Logic.Players;
    using Logic;

    public class CardTracker
    {
        private readonly ICollection<Card> remainingCards;
        private readonly ICollection<Card> playedCards;
        private readonly ICollection<Card> myRemainingTrumpCards;
        private readonly ICollection<Card> mySureCards;
        private readonly CardValidator cardValidator;

        public CardTracker()
        {
            this.MySureTrickPoints = 0;
            this.remainingCards = new List<Card>();
            this.playedCards = new HashSet<Card>();
            this.myRemainingTrumpCards = new List<Card>();
            this.mySureCards = new List<Card>();
            this.MyTrickPoints = 0;
            this.OpponentsTrickPoints = 0;
            this.cardValidator = new CardValidator();
        }

        public int OpponentsTrickPoints { get; set; }

        public int MyTrickPoints { get; set; }

        public int MySureTrickPoints { get; set; }

        public CardSuit TrumpSuit { get; set; }

        public ICollection<Card> MyRemainingTrumpCards
        {
            get
            {
                return this.myRemainingTrumpCards;
            }
        }

        public ICollection<Card> RemainingCards
        {
            get
            {
                return this.remainingCards;
            }
        }

        public ICollection<Card> MySureCards
        {
            get
            {
                return this.mySureCards;
            }
        }


        public void GetMyRemainingTrumpCards(ICollection<Card> myCards)
        {
            foreach (var card in myCards)
            {
                if(card.Suit == this.TrumpSuit)
                {
                    this.myRemainingTrumpCards.Add(card);
                }
            }
        }

        public void GetRemainingCards(ICollection<Card> myCards, Card trumpCard)
        {
            foreach (var card in myCards)
            {
                this.remainingCards.Remove(card);
            }

            foreach (var card in this.playedCards)
            {
                this.remainingCards.Remove(card);
            }

            if (trumpCard != null)
            {
                this.remainingCards.Remove(trumpCard);
            }
        }

        public void GetSureCardsWhenGameClosed(PlayerTurnContext context, ICollection<Card> cards, bool deckHasCards = true)
        {
            foreach (var myCard in cards)
            {
                var opponentsCardsInSuit = this.GetOpponentsCardInSuit(myCard.Suit);

                if (this.cardValidator.IsCardInAnnounce(context, myCard, cards, Announce.Forty)
                    || this.cardValidator.IsCardInAnnounce(context, myCard, cards, Announce.Twenty))
                {
                    continue;
                }

                if (!this.cardValidator.HasTrumpCard(context, this.RemainingCards)
                    && opponentsCardsInSuit.Count == 0 && myCard.Suit != this.TrumpSuit)
                {
                    mySureCards.Add(myCard);
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
                            if (!this.cardValidator.HasTrumpCard(context, this.RemainingCards))
                            {
                                this.MySureTrickPoints += (myCard.GetValue() + opponetCard.GetValue());
                                mySureCards.Add(myCard);
                            }
                        }
                        else
                        {
                            this.MySureTrickPoints += (myCard.GetValue() + opponetCard.GetValue());
                            mySureCards.Add(myCard);
                        }

                        break;
                    }
                }
            }
        }

        private ICollection<Card> GetOpponentsCardInSuit(CardSuit suit)
        {
            return this.RemainingCards.Where(c => c.Suit == suit)
                .OrderByDescending(c => c.GetValue()).ToList();
        }

        public void GetTrickPoints(PlayerTurnContext context)
        {
            this.MyTrickPoints = context.IsFirstPlayerTurn
                ? context.FirstPlayerRoundPoints
                : context.SecondPlayerRoundPoints;

            this.OpponentsTrickPoints = context.IsFirstPlayerTurn
                ? context.SecondPlayerRoundPoints
                : context.FirstPlayerRoundPoints;
        }

        public void ClearPlayedCards()
        {
            this.playedCards.Clear();
        }

        public void AddPlayedCard(Card card)
        {
            this.playedCards.Add(card);
        }

        public Card FindPlayedCard(CardType type, CardSuit suit)
        {
            return this.playedCards.FirstOrDefault(c => c.Type == type && c.Suit == suit);
        }

        public Card FindRemainingCard(CardType type, CardSuit suit)
        {
            return this.remainingCards.FirstOrDefault(c => c.Type == type && c.Suit == suit);
        }

        public Card FindMyRemainingTrumpCard(CardType type)
        {
            return this.MyRemainingTrumpCards.FirstOrDefault(c => c.Type == type);
        }

        public int CountPlayedCardsInSuit(CardSuit suit)
        {
            var counter = 0;
            foreach (var card in this.playedCards)
            {
                if (card.Suit == suit)
                {
                    counter++;
                }
            }

            return counter;
        }

        public void ClearRemainingCards()
        {
            this.remainingCards.Clear();
        }

        public void ClearMyRemainingTrumpCards()
        {
            this.myRemainingTrumpCards.Clear();
        }

        public void ClearMySureCards()
        {
            this.mySureCards.Clear();
        }

        //private void GetFullSuit(CardSuit suit)
        //{
        //    foreach (CardType type in Enum.GetValues(typeof(CardType)))
        //    {
        //        this.remainingCards.Add(new Card(suit, type));
        //    }
        //}

        public void GetFullDeck()
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardType type in Enum.GetValues(typeof(CardType)))
                {
                    this.remainingCards.Add(new Card(suit, type));
                }
            }
        }
    }
}