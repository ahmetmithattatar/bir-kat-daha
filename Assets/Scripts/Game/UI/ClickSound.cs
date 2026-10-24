using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>Butona tıklanınca kısa "tık" sesi. Sahne kurucu her butona ekler.</summary>
    [RequireComponent(typeof(Button))]
    public class ClickSound : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => Sfx.Play(SoundId.Click));
        }
    }
}
