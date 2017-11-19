using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ShieldEffectBOW : MonoBehaviour
{
    public ParticleSystem startEffectPrefab;
    public ParticleSystem endEffectPrefab;

    private readonly List<ParticleSystem> startEffects = new List<ParticleSystem>();
    private readonly List<ParticleSystem> endEffects = new List<ParticleSystem>();

    private bool isActive;

    void OnDisable()
    {
        SetActive(false);
    }

    public void SetEndEffectDelay(float duration)
    {
        if (endEffects.Count == 0)
            return;

        float endEffectDuration = endEffects[0].main.duration;
        duration = duration - endEffectDuration;

        this.Invoke(PlayEndEffect, duration);
    }

    public void Play()
    {
        SetEffects(startEffects, startEffectPrefab);
        SetEffects(endEffects, endEffectPrefab);
        SetActive(true);
    }

    private void PlayEndEffect()
    {
        if (!isActive)
            return;

        foreach (ParticleSystem startEffect in startEffects)
            startEffect.gameObject.SetActive(false);

        foreach (ParticleSystem endEffect in endEffects)
            endEffect.gameObject.SetActive(true);
    }

    private void SetEffects(List<ParticleSystem> effects, ParticleSystem effectPrefab)
    {
        if (effects.Count > 0)
            return;

        MeshFilter[] meshFilters = transform.root.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (!CheckLODLevel(meshFilter) || !CheckParts(meshFilter))
                continue;

            ParticleSystem effect = Instantiate(effectPrefab);

            effects.Add(effect);

            effect.transform.parent = meshFilter.transform;
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;
            effect.transform.localScale = Vector3.one;

            ParticleSystemRenderer particleSystemRenderer = effect.GetComponent<ParticleSystemRenderer>();
            particleSystemRenderer.mesh = meshFilter.mesh;

            effect.gameObject.SetActive(false);
        }
    }

    private void SetActive(bool value)
    {
        isActive = value;

        foreach (ParticleSystem startEffect in startEffects)
            startEffect.gameObject.SetActive(value);

        foreach (ParticleSystem endEffect in endEffects)
            endEffect.gameObject.SetActive(false);
    }

    private bool CheckLODLevel(MeshFilter meshFilter)
    {
        string lodLevelString = Regex.Match(meshFilter.transform.name, @"(\d+)$").Groups[1].ToString();

        int lodLevel;

        if (!int.TryParse(lodLevelString, out lodLevel))
            return true;

        return lodLevel < 1;
    }

    private bool CheckParts(MeshFilter meshFilter)
    {
        string[] exclusions = { "rotor", "propeller", "sticker" };

        foreach (string exclusion in exclusions)
        {
            if (meshFilter.transform.name.ToLower().Contains(exclusion))
                return false;
        }

        return true;
    }
}
