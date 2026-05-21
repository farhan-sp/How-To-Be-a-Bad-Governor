using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeSpriteIntro : MonoBehaviour
{
    [Header("Referensi UI")]
    [SerializeField] private Image komponenImageUI;

    [Header("Aset Gambar")]
    [SerializeField] private Sprite gambarPertama;
    [SerializeField] private Sprite gambarKedua;

    [Header("Pengaturan Waktu (Detik)")]
    [SerializeField] private float durasiTampilGambar = 2f; // Lama gambar diam setelah muncul penuh
    [SerializeField] private float kecepatanFade = 1.5f;     // Semakin tinggi, semakin cepat memudar
    [SerializeField] private string namaSceneBerikutnya = "MainMenu";

    void Start()
    {
        // Memastikan gambar mulai dari kondisi transparan (Alpha = 0)
        SetAlpha(0f);
        StartCoroutine(AlurIntroSDF());
    }

    IEnumerator AlurIntroSDF()
    {
        // ==========================================
        // SIKLUS GAMBAR 1
        // ==========================================
        komponenImageUI.sprite = gambarPertama;
        
        // Fade In Gambar 1
        yield return StartCoroutine(ProsesFade(1f));
        
        // Diam selama beberapa detik
        yield return new WaitForSeconds(durasiTampilGambar);
        
        // Fade Out Gambar 1
        yield return StartCoroutine(ProsesFade(0f));


        // ==========================================
        // SIKLUS GAMBAR 2
        // ==========================================
        komponenImageUI.sprite = gambarKedua;
        
        // Fade In Gambar 2
        yield return StartCoroutine(ProsesFade(1f));
        
        // Diam selama beberapa detik
        yield return new WaitForSeconds(durasiTampilGambar);
        
        // Fade Out Gambar 2
        yield return StartCoroutine(ProsesFade(0f));


        // Pindah ke Main Menu setelah semua selesai
        SceneManager.LoadScene(namaSceneBerikutnya);
    }

    // Fungsi pembantu untuk memproses perubahan Alpha secara bertahap
    IEnumerator ProsesFade(float targetAlpha)
    {
        Color warnaSekarang = komponenImageUI.color;

        // Loop berjalan selama Alpha saat ini belum mencapai Target Alpha
        while (!Mathf.Approximately(warnaSekarang.a, targetAlpha))
        {
            warnaSekarang.a = Mathf.MoveTowards(warnaSekarang.a, targetAlpha, kecepatanFade * Time.deltaTime);
            komponenImageUI.color = warnaSekarang;
            yield return null; // Tunggu ke frame berikutnya sebelum melanjutkan loop
        }
    }

    // Fungsi instan untuk mengubah Alpha tanpa animasi
    void SetAlpha(float nilaiAlpha)
    {
        Color warna = komponenImageUI.color;
        warna.a = nilaiAlpha;
        komponenImageUI.color = warna;
    }
}