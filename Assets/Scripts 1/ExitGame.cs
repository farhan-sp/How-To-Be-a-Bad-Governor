using UnityEngine;

public class ExitGame : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Exit()
    {
        Debug.Log("Game dipaksa keluar via tombol.");
        Application.Quit();
    }
}
