using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Data
{
    public class GameData : MonoBehaviour
    {
        public static GameData Instance;
        
        [Header("Params")]
        [SerializeField] private int m_rowCount;
        [SerializeField] private int m_columnCount;
        [SerializeField] private Vector2 m_topLeftPos;
        [SerializeField] private int comboCount;
        
        [Header("Refs")]
        [SerializeField] private GameObject m_cardPrefab;
        [SerializeField] private CardTypeSO m_cardTypesSO;
        
        [Header("Objs")] 
        [SerializeField] private Transform m_uIParent;
        [SerializeField] private LineRenderer m_lineRenderer;
        
        // Data
        [HideInInspector] public List<CardData> selectedCards;
        private CardData[,] m_allCards;
        private List<CardData> m_valuedCards;

        private Camera mainCam;
        
        private void Start()
        {
            Instance = this;
            selectedCards = new List<CardData>();
            mainCam = Camera.main;
            
            InitCards();
            InitGame();
        }
        
        private void InitCards()
        {
            m_allCards = new CardData[m_rowCount, m_columnCount];

            for (int i = 0; i < m_rowCount; i++)
            {
                for (int j = 0; j < m_columnCount; j++)
                {
                    GameObject card = Instantiate(m_cardPrefab, m_uIParent);
                    CardData data = card.GetComponent<CardData>();
                    card.transform.localPosition = new Vector3(m_topLeftPos.x + j * data.cardWidth, m_topLeftPos.y - i * data.cardHeight, 0);
                    card.name = $"{i},{j}";
                    data.cardIndex = new Vector2Int(i, j);
                    m_allCards[i, j] = data;
                }
            }
        }

        private void InitGame()
        {
            m_valuedCards = new List<CardData>();
            comboCount = comboCount * 2 < (m_rowCount - 2) * (m_columnCount - 2) ? comboCount : (m_rowCount - 2) * (m_columnCount - 2) / 2;

            for (int i = 0; i < comboCount; i++)
            {
                CardType type = m_cardTypesSO.allCardTypes[i % m_cardTypesSO.allCardTypes.Length];
                for (int j = 0; j < 2; j++)
                {
                    CardData card = RandomChooseAnEmptyCard();
                    card.cardValue = type.cardValue;
                    card.SetImage(type.cardSprite, type.cardColor);
                    m_valuedCards.Add(card);
                }
            }
        }

        private CardData RandomChooseAnEmptyCard()
        {
            int randomXIndex = -1;
            int randomYIndex = -1;

            bool hasFound = false;
            while (!hasFound)
            {
                randomXIndex = Random.Range(1, m_rowCount - 1);
                randomYIndex = Random.Range(1, m_columnCount - 1);

                bool isExisted = false;
                
                foreach (CardData card in m_valuedCards)
                {
                    if (card.cardIndex == new Vector2Int(randomXIndex, randomYIndex))
                    {
                        isExisted = true;
                        break;
                    }
                }

                if (!isExisted)
                    hasFound = true;
            }

            return m_allCards[randomXIndex, randomYIndex];
        }

        public void CheckSelectedCards()
        {
            if (selectedCards.Count == 2) 
            {
                CardData card1 = selectedCards[0];
                CardData card2 = selectedCards[1];
                
                // Check if the same card
                if (card1.cardValue == card2.cardValue)
                {
                    // Check path
                    if (CheckCombo(card1, card2))
                    {
                        // Combo
                        StartCoroutine(DelayComboCards(card1, card2));
                    }
                    else
                    {
                        // Cancel
                        card1.SetToggle(false);
                        card2.SetToggle(false);
                    }
                }
                else
                {
                    // Cancel
                    card1.SetToggle(false);
                    card2.SetToggle(false);
                }

                selectedCards.Remove(card1);
                selectedCards.Remove(card2);
            }
        }

        private IEnumerator DelayComboCards(params CardData[] cards)
        {
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].cardValue = 0;
            }
        }

        private bool CheckCombo(CardData card1, CardData card2)
        {
            // Find path
            List<CardData> path = FindPath(card1, card2);
            
            if (path == null)
                return false;
            
            // Draw path
            m_lineRenderer.gameObject.SetActive(true);
            m_lineRenderer.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                // Debug.Log($"path: {i}-({path[i].cardIndex.x}, {path[i].cardIndex.y})");
                Vector3 worldPos = mainCam.ScreenToWorldPoint(path[i].transform.position);
                m_lineRenderer.SetPosition(i, new Vector3(worldPos.x, worldPos.y, 0));
            }
            
            return true;
        }

        private List<CardData> FindPath(CardData startCard, CardData endCard)
        {
            List<CardData> path = new List<CardData>();
            
            // 1. No turning points
            
            // Check if the two cards are on the same row
            if (HorizontalCheckCombo(startCard.cardIndex, endCard.cardIndex))
            {
                path.Add(startCard);
                path.Add(endCard);
                return path;
            }
            
            // Check if the two cards are on the same column
            if (VerticalCheckCombo(startCard.cardIndex, endCard.cardIndex))
            {
                path.Add(startCard);
                path.Add(endCard);
                return path;
            }
            
            // 2. One turning point
            if (OneTurningCheckCombo(startCard.cardIndex, endCard.cardIndex, out Vector2Int turningPos))
            {
                path.Add(startCard);
                path.Add(m_allCards[turningPos.x, turningPos.y]);
                path.Add(endCard);
                return path;
            }
            
            // 3. Two turning points
            if (TwoTurningsCheckCombo(startCard.cardIndex, endCard.cardIndex, out Vector2Int turningPos1, out Vector2Int turningPos2))
            {
                path.Add(startCard);
                path.Add(m_allCards[turningPos1.x, turningPos1.y]);
                path.Add(m_allCards[turningPos2.x, turningPos2.y]);
                path.Add(endCard);
                return path;
            }

            return null;
        }

        private bool IsBlocked(int indexX, int indexY)
        {
            if (m_allCards[indexX, indexY].cardValue > 0)
                return true;

            return false;
        }

        private bool HorizontalCheckCombo(Vector2Int startPos, Vector2Int endPos)
        {
            // A _ _ B
            
            // Start and end cards are exactly the same card
            if (startPos.x == endPos.x && startPos.y == endPos.y)
                return false;
            
            // Not on the same row
            if (startPos.x != endPos.x)
                return false;
            
            // On the same row:
            int minY = Mathf.Min(startPos.y, endPos.y);
            int maxY = Mathf.Max(startPos.y, endPos.y);
            
            // 1. Check if neighboured
            if (minY + 1 == maxY)
                return true;
            
            // 2. Check if there's block between
            for (int i = minY + 1; i < maxY; i++)
            {
                if (IsBlocked(startPos.x, i))
                    return false;
            }

            return true;
        }

        private bool VerticalCheckCombo(Vector2Int startPos, Vector2Int endPos)
        {
            // A
            // _
            // _
            // B
            
            // Start and end cards are exactly the same card
            if (startPos.x == endPos.x && startPos.y == endPos.y)
                return false;
            
            // Not on the same column
            if (startPos.y != endPos.y)
                return false;
            
            // On the same column:
            int minX = Mathf.Min(startPos.x, endPos.x);
            int maxX = Mathf.Max(startPos.x, endPos.x);

            // 1. Check if neighboured
            if (minX + 1 == maxX)
                return true;

            // 2. Check if there's block between
            for (int i = minX + 1; i < maxX; i++)
            {
                if (IsBlocked(i, startPos.y))
                    return false;
            }

            return true;
        }

        private bool OneTurningCheckCombo(Vector2Int startPos, Vector2Int endPos, out Vector2Int turningPos)
        {
            // For the two points: Left top & Bottom right
            // Either one of them satisfies both Horizontal and Vertical check will be okay
            
            // C _ _ B
            // _ _ _ _
            // A _ _ C

            turningPos = new Vector2Int(0, 0);
            
            // Start and end cards are exactly the same card
            if (startPos.x == endPos.x && startPos.y == endPos.y)
                return false;

            if (!IsBlocked(startPos.x, endPos.y))
            {
                turningPos = new Vector2Int(startPos.x, endPos.y);
                if (HorizontalCheckCombo(turningPos, startPos) && VerticalCheckCombo(turningPos, endPos))
                    return true;
            }

            if (!IsBlocked(endPos.x, startPos.y))
            {
                turningPos = new Vector2Int(endPos.x, startPos.y);
                if (HorizontalCheckCombo(turningPos, endPos) && VerticalCheckCombo(turningPos, startPos))
                    return true;
            }

            return false;
        }
        
        private struct PossibleRoute : IComparable<PossibleRoute>
        {
            public Vector2Int turningPos1;
            public Vector2Int turningPos2;
            public int distance;

            public PossibleRoute(Vector2Int turningPos1, Vector2Int turningPos2, int distance)
            {
                this.turningPos1 = turningPos1;
                this.turningPos2 = turningPos2;
                this.distance = distance;
            }
            
            public int CompareTo(PossibleRoute other)
            {
                return distance.CompareTo(other.distance);
            }
        }
        
        private bool TwoTurningsCheckCombo(Vector2Int startPos, Vector2Int endPos, out Vector2Int turningPos1, out Vector2Int turningPos2)
        {
            // Check all the points on the lines that go through startPos(A) or endPos(B) => C
            // Either satisfy both
            //      1. A and C satisfy Horizontal or Vertical combo 
            //      2. C and B satisfy OneTurning combo
            // Or satisfy both
            //      1. A and C satisfy OneTurning combo
            //      2. C and B satisfy Horizontal or Vertical combo 
            
            turningPos1 = new Vector2Int(0, 0);
            turningPos2 = new Vector2Int(0, 0);
            
            // Start and end cards are exactly the same card
            if (startPos.x == endPos.x && startPos.y == endPos.y)
                return false;
            
            List<PossibleRoute> allPossibleRoutes = new List<PossibleRoute>();
            
            // Two horizontal lines
            for (int i = 0; i < m_columnCount; i++)
            {
                // A Horizontal Line
                if (i != startPos.y)
                {
                    // A & C => Horizontal
                    // C & B => One Turning
                    Vector2Int turning1 = new Vector2Int(startPos.x, i);
                    if (!IsBlocked(turning1.x, turning1.y) && HorizontalCheckCombo(startPos, turning1) && OneTurningCheckCombo(turning1, endPos, out Vector2Int turning2))
                        AddPossibleRoute(startPos, turning1, turning2, endPos, allPossibleRoutes);
                }
                
                // B Horizontal Line
                if (i != endPos.y)
                {
                    // A & C => One Turning
                    // C & B => Horizontal
                    Vector2Int turning2 = new Vector2Int(endPos.x, i);
                    if (!IsBlocked(turning2.x, turning2.y) && OneTurningCheckCombo(startPos, turning2, out Vector2Int turning1) && HorizontalCheckCombo(turning2, endPos))
                        AddPossibleRoute(startPos, turning1, turning2, endPos, allPossibleRoutes);
                }
            }
            
            // Two vertical lines
            for (int i = 0; i < m_rowCount; i++)
            {
                // A Vertical Line
                if (i != startPos.x)
                {
                    // A & C => Vertical
                    // C & B => One Turning
                    Vector2Int turning1 = new Vector2Int(i, startPos.y);
                    if (!IsBlocked(turning1.x, turning1.y) && VerticalCheckCombo(startPos, turning1) && OneTurningCheckCombo(turning1, endPos, out Vector2Int turning2))
                        AddPossibleRoute(startPos, turning1, turning2, endPos, allPossibleRoutes);
                }
                
                // B Vertical Line
                if (i != endPos.x)
                {
                    // A & C => One Turning
                    // C & B => Vertical
                    Vector2Int turning2 = new Vector2Int(i, endPos.y);
                    if (!IsBlocked(turning2.x, turning2.y) && OneTurningCheckCombo(startPos, turning2, out Vector2Int turning1) && VerticalCheckCombo(turning2, endPos))
                        AddPossibleRoute(startPos, turning1, turning2, endPos, allPossibleRoutes);
                }
            }

            if (allPossibleRoutes.Count == 0)
                return false;

            // Find the shortest possible route
            allPossibleRoutes.Sort();

            PossibleRoute bestRoute = allPossibleRoutes[0];
            turningPos1 = bestRoute.turningPos1;
            turningPos2 = bestRoute.turningPos2;

            return true;
        }

        private void AddPossibleRoute(Vector2Int startPos, Vector2Int turning1, Vector2Int turning2, Vector2Int endPos, List<PossibleRoute> allPossibleRoutes)
        {
            int distance = Mathf.Abs(turning1.y - startPos.y) + Mathf.Abs(turning2.x - turning1.x) + Mathf.Abs(turning2.y - endPos.y);
            allPossibleRoutes.Add(new PossibleRoute(turning1, turning2, distance));
        }
    } 
}
