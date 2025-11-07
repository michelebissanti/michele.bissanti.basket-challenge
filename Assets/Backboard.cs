using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Backboard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bonusText;
    [SerializeField] private TextMeshProUGUI bonusText2;
    [SerializeField] private Color bonusColor;

    private Renderer rendererMat;
    private Material[] materials;

    void Start()
    {
        rendererMat = GetComponent<Renderer>();
        materials = rendererMat.materials;
        HideText();
    }

    private void OnEnable()
    {
        GameManager.OnBackboardBonusActivated += OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired += OnBackboardBonusExpired;
    }

    private void OnDisable()
    {
        GameManager.OnBackboardBonusActivated -= OnBackboardBonusActivated;
        GameManager.OnBackboardBonusExpired -= OnBackboardBonusExpired;
    }

    private void OnBackboardBonusActivated(int bonusPoints)
    {
        materials[2].SetColor("_BaseColor", bonusColor);
        rendererMat.materials = materials;
        ShowText(bonusPoints);
    }

    private void OnBackboardBonusExpired()
    {
        materials[2].SetColor("_BaseColor", Color.white);
        rendererMat.materials = materials;
        HideText();
    }

    private void ShowText(int bonusPoints)
    {
        bonusText.gameObject.SetActive(true);
        bonusText2.gameObject.SetActive(true);
        bonusText.text = $"+{bonusPoints}";
        bonusText2.text = $"+{bonusPoints}";
    }

    private void HideText()
    {
        bonusText.gameObject.SetActive(false);
        bonusText2.gameObject.SetActive(false);
    }
}
