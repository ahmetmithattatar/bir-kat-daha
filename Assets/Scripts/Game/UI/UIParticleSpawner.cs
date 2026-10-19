using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RiskLoop.Game.UI
{
    /// <summary>
    /// Basit UI parçacıkları: kıvılcım, moloz, toz, çakıl. Şablon Image kopyalanır, uçar, söner, silinir.
    /// Kazı sahnesinin içinde durur, sahnenin maskesi taşan parçaları keser.
    /// </summary>
    public class UIParticleSpawner : MonoBehaviour
    {
        [SerializeField] private Image template;  // Kapalı şablon (küçük kare)

        /// <summary>
        /// worldPos noktasından count adet parçacık.
        /// speed: rastgele yöndeki hız, baseVelocity: hepsine eklenen ortak hız (ör. aşağı düşüş), gravity: aşağı çekim.
        /// </summary>
        public void Spawn(Vector3 worldPos, Color color, int count, float speed,
                          float gravity = -1200f, float minSize = 8f, float maxSize = 20f,
                          float lifetime = 0.8f, Vector2 baseVelocity = default, bool rotate = true)
        {
            for (int i = 0; i < count; i++)
            {
                Image p = Instantiate(template, transform);
                p.gameObject.SetActive(true);
                p.color = color;
                float size = Random.Range(minSize, maxSize);
                p.rectTransform.sizeDelta = new Vector2(size, size);
                p.transform.position = worldPos;
                p.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
                Vector2 velocity = baseVelocity + Random.insideUnitCircle * speed;
                StartCoroutine(Fly(p, velocity, gravity, lifetime * Random.Range(0.7f, 1.2f), rotate));
            }
        }

        private IEnumerator Fly(Image p, Vector2 velocity, float gravity, float lifetime, bool rotate)
        {
            RectTransform rt = p.rectTransform;
            Color c = p.color;
            float startAlpha = c.a;
            float t = 0f;
            while (t < lifetime)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                velocity.y += gravity * dt;
                rt.anchoredPosition += velocity * dt;
                if (rotate) rt.Rotate(0f, 0f, 400f * dt);
                float k = t / lifetime;
                c.a = startAlpha * (k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
                p.color = c;
                yield return null;
            }
            Destroy(p.gameObject);
        }
    }
}
