using Yarn.Unity;
using UnityEngine;

public class DialogueEffects : MonoBehaviour
{
    public CharacterFade characterA;
    public CharacterFade characterB;

    [YarnCommand("highlight")]
    public void HighlightCharacter(string characterName)
    {
        switch (characterName)
        {
            case "A":
                characterA.TintTo(1f);
                characterB.TintTo(0.3f);
                break;

            case "B":
                characterA.TintTo(0.3f);
                characterB.TintTo(1f);
                break;
                
            case "None":
                characterA.TintTo(1f);
                characterB.TintTo(1f);
                break;
        }
    }
}