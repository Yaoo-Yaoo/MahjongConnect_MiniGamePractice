using UnityEngine;
using UnityEngine.UI;

namespace Game.Data
{
    public class CardData : MonoBehaviour
    {
        // Data
        [HideInInspector] public float cardWidth;
        [HideInInspector] public float cardHeight;

        [Header("Data")] 
        public Vector2Int cardIndex;
        [SerializeField] private int m_cardValue;

        [Header("Components")] 
        [SerializeField] private GameObject m_SelectedEffect;
        
        // Components
        private Toggle cardToggle;
        private Image cardImage;
        
        public int cardValue
        {
            get => m_cardValue;
            set
            {
                m_cardValue = value;
                if (value == 0)
                {
                    cardImage.color = new Color(1, 1, 1, 0);
                    cardToggle.interactable = false;
                    m_SelectedEffect.SetActive(false);
                }
                else
                {
                    cardToggle.interactable = true;
                }
            }
        }

        private void Awake()
        {
            // Components
            cardImage = GetComponent<Image>();
            cardToggle = GetComponent<Toggle>();
            cardToggle.onValueChanged.AddListener(OnCardSelectedStatusChanged);
            
            // Data
            InitCard();
        }

        private void OnDestroy()
        {
            cardToggle.onValueChanged.RemoveAllListeners();
        }

        private void InitCard()
        {
            Vector2 sizeDelta = GetComponent<RectTransform>().sizeDelta;
            cardWidth = sizeDelta.x;
            cardHeight = sizeDelta.y;
            
            cardValue = 0;
            cardToggle.isOn = false;
        }
        
        private void OnCardSelectedStatusChanged(bool isSelected)
        {
            m_SelectedEffect.SetActive(isSelected);

            if (isSelected)
            {
                // Select the card and check
                GameData.Instance.selectedCards.Add(this);
                GameData.Instance.CheckSelectedCards();
            }
            else
            {
                // Unselect the card
                GameData.Instance.selectedCards.Remove(this);
            }
        }

        public void SetImage(Sprite sprite, Color color)
        {
            cardImage.sprite = sprite;
            cardImage.color = color;
        }

        public void SetToggle(bool status)
        {
            cardToggle.isOn = status;
        }
    }
}
