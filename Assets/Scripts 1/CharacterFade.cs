using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CharacterFade : MonoBehaviour
{
    public Image characterImage;
    public float transitionDuration = 0.5f;

    public void TintTo(float brightness)
    {
        if (characterImage == null) return;
        
        StopAllCoroutines(); // Menghentikan transisi lama jika ada transisi baru
        StartCoroutine(TintRoutine(brightness));
    }

    private IEnumerator TintRoutine(float targetBrightness)
    {
        Color startColor = characterImage.color;
        Color targetColor = new Color(targetBrightness, targetBrightness, targetBrightness, startColor.a);
        
        float time = 0;

        while (time < transitionDuration)
        {
            characterImage.color = Color.Lerp(startColor, targetColor, time / transitionDuration);
            time += Time.deltaTime;
            yield return null;
        }

        characterImage.color = targetColor;
    }
}
