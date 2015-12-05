namespace Santase.AI.NinjaPlayer.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Logic.Cards;
    using Logic.Players;

    public class CardTracker
    {
        private readonly ICollection<Card> remainingCards;
        private readonly ICollection<Card> playedCards;

        public CardTracker()
        {
            this.MySureTrickPoints = 0;
            this.remainingCards = new List<Card>();
            this.playedCards = new List<Card>();
            this.MyTrickPoints = 0;
            this.OpponentsTrickPoints = 0;
            this.GetFullDeck();
        }

        public int OpponentsTrickPoints { get; set; }

        public int MyTrickPoints { get; set; }

        public int MySureTrickPoints { get; set; }

        public ICollection<Card> RemainingCards
        {
            get
            {
                return this.remainingCards;
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

        //private void GetFullSuit(CardSuit suit)
        //{
        //    foreach (CardType type in Enum.GetValues(typeof(CardType)))
        //    {
        //        this.remainingCards.Add(new Card(suit, type));
        //    }
        //}

        private void GetFullDeck()
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