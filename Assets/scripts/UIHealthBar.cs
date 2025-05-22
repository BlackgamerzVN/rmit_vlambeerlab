using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private Image fillImage;

    [Header("Health Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    public void SetMaxHealth(int maxHealth)
    {
        _slider.maxValue = maxHealth;
        _slider.value = maxHealth;
    }

    public void SetHealth(int currentHealth)
    {
        _slider.value = currentHealth;
        UpdateFillColor();
    }
    // Optional: smooth animation
    public void AnimateHealth(int targetHealth, float duration = 0.2f)
    {
        StopAllCoroutines();
        StartCoroutine(SmoothHealthChange(targetHealth, duration));
        UpdateFillColor();
    }

    private IEnumerator SmoothHealthChange(int target, float duration)
    {
        float start = _slider.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _slider.value = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        _slider.value = target;
    }
    private void UpdateFillColor()
    {
        if (fillImage == null || _slider == null) return;

        float percent = _slider.value / _slider.maxValue;

        if (percent > 0.8f)
            fillImage.color = healthyColor;
        else if (percent > 0.5f)
            fillImage.color = warningColor;
        else
            fillImage.color = criticalColor;
    }
}
