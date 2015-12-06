namespace Santase.AI.NinjaPlayer.ChooseCardStrategies.Contracts
{
    using Logic.Cards;
    using Santase.Logic.Players;
    using System.Collections.Generic;

    public interface IChooseCardStrategy
    {
        PlayerAction ChooseCard(PlayerTurnContext context, ICollection<Card> crads);
    }
}
