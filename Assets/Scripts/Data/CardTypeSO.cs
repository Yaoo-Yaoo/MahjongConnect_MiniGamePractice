using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "CardTypes", menuName = "Create SO/Create CardTypeSO")]
    public class CardTypeSO : ScriptableObject
    {
        public CardType[] allCardTypes;
    }
}
