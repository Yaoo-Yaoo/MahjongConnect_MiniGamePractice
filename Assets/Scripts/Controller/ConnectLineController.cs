using System;
using UnityEngine;

namespace Game
{
    public class ConnectLineController : MonoBehaviour
    {
        private LineRenderer m_lineRenderer;
        
        private void Awake()
        {
            m_lineRenderer = GetComponent<LineRenderer>();
        }

        private void OnEnable()
        {
            // Auto reset after 1s
            Invoke("Reset", 0.5f);
        }

        private void Reset()
        {
            m_lineRenderer.positionCount = 0;
            gameObject.SetActive(false);
        }
    }
}
