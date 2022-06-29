using UnityEngine;
using UnityEngine.Rendering;

namespace Kino.Aqua {

#region Internal classes

static class CommonAssets
{
    static Texture2D LoadNoiseTexture()
      => Resources.Load<Texture2D>("KinoAquaNoise");

    static Texture2D _noiseTexture;

    public static Texture2D NoiseTexture
      => _noiseTexture = _noiseTexture ?? LoadNoiseTexture();
}

static class ShaderIDs
{
    internal static int TempTexture1 = Shader.PropertyToID("_TempTexture1");

    internal static readonly int SourceTexture = Shader.PropertyToID("_SourceTexture");

    internal static readonly int InputTexture = Shader.PropertyToID("_InputTexture");

    internal static readonly int WobblingPower = Shader.PropertyToID("_WobblingPower");
    internal static readonly int WobblingTiling = Shader.PropertyToID("_WobblingTiling");
    internal static readonly int WobblingTexture = Shader.PropertyToID("_WobblingTexture");
    internal static readonly int EdgePower = Shader.PropertyToID("_EdgePower");
    internal static readonly int EdgeSize = Shader.PropertyToID("_EdgeSize");
    
    internal static readonly int PaperPower = Shader.PropertyToID("_PaperPower");
    internal static readonly int PaperTiling = Shader.PropertyToID("_PaperTiling");
    internal static readonly int PaperTexture = Shader.PropertyToID("_PaperTexture");
}

#endregion

#region Public classes

public enum OverlayMode { Off, Multiply, Overlay, Screen }

public static class ShaderHelper
{
    public static void SetProperties
        (Material material,
        Texture inputTexture,

        float wobblingPower,
        Vector2 wobblingTiling,

        float opacity,
        Color edgeColor,
        float edgeContrast,
        Color fillColor,
        float blurWidth,
        float blurFrequency,
        float hueShift,
        float interval,
        int iteration)
    {
        material.SetFloat(ShaderIDs.WobblingPower, wobblingPower.value);
        material.SetVector(ShaderIDs.WobblingTiling, wobblingTiling.value);
        material.SetTexture(ShaderIDs.WobblingTexture, _wobblingTexture);
        material.SetTexture(ShaderIDs.SourceTexture, srcRT);

        /*
        var bfreq = Mathf.Exp((blurFrequency - 0.5f) * 6);

        material.SetVector(ShaderIDs.EffectParams1,
          new Vector4(opacity, interval,blurWidth, bfreq));

        material.SetVector(ShaderIDs.EffectParams2,
          new Vector2(edgeContrast, hueShift));

        material.SetColor(ShaderIDs.EdgeColor, edgeColor);
        material.SetColor(ShaderIDs.FillColor, fillColor);
        material.SetInt(ShaderIDs.Iteration, iteration);

        material.SetTexture(ShaderIDs.MainTex, inputTexture);
        material.SetTexture(ShaderIDs.NoiseTexture, CommonAssets.NoiseTexture);
        */
    }

    public static void SetOverlayProperties
      (Material material, OverlayMode mode, Texture texture, float opacity)
    {
        if (mode == OverlayMode.Multiply)
            material.EnableKeyword("KINO_AQUA_MULTIPLY");
        else
            material.DisableKeyword("KINO_AQUA_MULTIPLY");

        if (mode == OverlayMode.Overlay)
            material.EnableKeyword("KINO_AQUA_OVERLAY");
        else
            material.DisableKeyword("KINO_AQUA_OVERLAY");

        if (mode == OverlayMode.Screen)
            material.EnableKeyword("KINO_AQUA_SCREEN");
        else
            material.DisableKeyword("KINO_AQUA_SCREEN");

        if (mode == OverlayMode.Off)
            material.EnableKeyword("KINO_AQUA_OFF");
        else
            material.DisableKeyword("KINO_AQUA_OFF");

        material.SetTexture(ShaderIDs.OverlayTexture, texture);
        material.SetFloat(ShaderIDs.OverlayOpacity, opacity);
    }
}

#endregion

} // namespace Kino.Aqua
