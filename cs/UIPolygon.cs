/******************************************************************************
 *  作者 : <LIJIJIAN>
 *  版本 : 
 *  创建时间: 
 *  文件描述: 
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using LuaInterface;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

/// <summary>
/// 作者：<LIJIJIAN>
/// 说明：
/// 
/// </summary>
public class UIPolygon : Graphic
{
    public Sprite activeSprite;

    public override Texture mainTexture
    {
        get
        {
            if (activeSprite == null)
            {
                if (material != null && material.mainTexture != null)
                {
                    return material.mainTexture;
                }
                return s_WhiteTexture;
            }

            return activeSprite.texture;
        }
    }

    public float pixelsPerUnit
    {
        get
        {
            float spritePixelsPerUnit = 100;
            if (activeSprite)
                spritePixelsPerUnit = activeSprite.pixelsPerUnit;

            float referencePixelsPerUnit = 100;
            if (canvas)
                referencePixelsPerUnit = canvas.referencePixelsPerUnit;

            return spritePixelsPerUnit / referencePixelsPerUnit;
        }
    }

    protected static Material s_ETC1DefaultUI = null;
    public static Material defaultETC1GraphicMaterial
    {
        get
        {
            if (s_ETC1DefaultUI == null)
                s_ETC1DefaultUI = Canvas.GetETC1SupportedCanvasMaterial();
            return s_ETC1DefaultUI;
        }
    }

    public override Material material
    {
        get
        {
            if (m_Material != null)
                return m_Material;
#if UNITY_EDITOR
            if (Application.isPlaying && activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                return defaultETC1GraphicMaterial;
#else

                if (activeSprite && activeSprite.associatedAlphaSplitTexture != null)
                    return defaultETC1GraphicMaterial;
#endif

            return defaultMaterial;
        }

    }


    private float iTwo = 1.0f / 2.0f;

    [Range(0, Mathf.PI * 2)]
    public float Rad = 0;

    private const int MAX_POINT_NUM = 10;
    [Range(3, MAX_POINT_NUM)]
    public int VertexNum = 3;
    private readonly Vector3[] vertexes = new Vector3[MAX_POINT_NUM + 1];
    private readonly Vector2[] uvs = new Vector2[MAX_POINT_NUM + 1];
    private readonly float[] values = new float[MAX_POINT_NUM] {1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f};


    private float percent = 1.0f;

    [ContextMenu("SetNativeSize")]
    public override void SetNativeSize()
    {
        if (activeSprite != null)
        {
            float w = activeSprite.rect.width / pixelsPerUnit;
            float h = activeSprite.rect.height / pixelsPerUnit;
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.sizeDelta = new Vector2(w, h);
            SetAllDirty();
        }
    }

    public void ShowPoints(bool animation, params object[] args)
    {
        int num = Mathf.Min(args.Length, MAX_POINT_NUM);
        for (int i = 0; i < num; ++i)
        {
            values[i] = (float)(double)args[i];
        }

        VertexNum = num;

        percent = animation ? 0 : 1;
        SetVerticesDirty();
    }

    [ContextMenu("Show")][NoToLua]
    public void Show()
    {

        ShowPoints(true, 1.0, 0.8, 0.7, 0.2, 0.6, 0.2, 0.8);
        /*
        int num = VertexNum;
        VertexNum = num;
        percent = 0;
        SetVerticesDirty();
        */
    }

    // 开始播放动画
    [ContextMenu("PlayAnimation")][NoToLua]
    public void PlayAnimation()
    {
        percent = 0;
        SetVerticesDirty();
    }

    void Update()
    {
        if (percent < 1)
        {
            percent = Mathf.Clamp(percent + Time.deltaTime, 0, 1);
            SetVerticesDirty();
        }
    }

    /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
    private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
    {
        var padding = activeSprite == null ? Vector4.zero : DataUtility.GetPadding(activeSprite);
        var size = activeSprite == null ? Vector2.zero : new Vector2(activeSprite.rect.width, activeSprite.rect.height);

        Rect r = GetPixelAdjustedRect();
        // Debug.Log(string.Format("r:{2}, size:{0}, padding:{1}", size, padding, r));

        int spriteW = Mathf.RoundToInt(size.x);
        int spriteH = Mathf.RoundToInt(size.y);

        var v = new Vector4(
                padding.x / spriteW,
                padding.y / spriteH,
                (spriteW - padding.z) / spriteW,
                (spriteH - padding.w) / spriteH);

        if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
        {
            var spriteRatio = size.x / size.y;
            var rectRatio = r.width / r.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = r.height;
                r.height = r.width * (1.0f / spriteRatio);
                r.y += (oldHeight - r.height) * rectTransform.pivot.y;
            }
            else
            {
                var oldWidth = r.width;
                r.width = r.height * spriteRatio;
                r.x += (oldWidth - r.width) * rectTransform.pivot.x;
            }
        }

        v = new Vector4(
                r.x + r.width * v.x,
                r.y + r.height * v.y,
                r.x + r.width * v.z,
                r.y + r.height * v.w
                );

        return v;
    }

    /// <summary>
    /// Generate vertices for a simple Image.
    /// </summary>
    void GenerateSimpleSprite(VertexHelper vh, bool lPreserveAspect)
    {
        Vector4 v = Vector4.zero;
        Vector4 uv = new Vector4(0, 0, 1, 1);
        if (activeSprite == null)
        {
            Rect r = GetPixelAdjustedRect();
            v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
        }
        else
        {
            v = GetDrawingDimensions(lPreserveAspect);
            uv = DataUtility.GetOuterUV(activeSprite);
        }

        float vValue = Mathf.Min(Mathf.Abs(v.z - v.x), Mathf.Abs(v.y - v.w)) * iTwo;
        Vector3 vCenter = new Vector3((v.x + v.z) * iTwo, (v.y + v.w) * iTwo);
        vertexes[0] = vCenter;

        float uValue = Mathf.Min(Mathf.Abs(uv.z - uv.x), Mathf.Abs(uv.y - uv.w)) * iTwo;
        Vector2 uCenter = new Vector3((uv.x + uv.z) * iTwo, (uv.y + uv.w) * iTwo);
        uvs[0] = uCenter;


        float deltaRad = Mathf.PI / VertexNum * 2;

        vh.Clear();
        vh.AddVert(vertexes[0], color, uvs[0]);

        for (int i = 1; i < VertexNum + 1; ++i)
        {
            float rad = deltaRad*(i - 1) + Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            vertexes[i] = new Vector3(sin, cos) * vValue * Mathf.Min(values[i - 1], percent)  + vCenter;
            uvs[i] = new Vector2(sin, cos) * uValue * Mathf.Min(values[i - 1], percent) + uCenter;

            vh.AddVert(vertexes[i], color, uvs[i]);

            if (i > 1)
            {
                vh.AddTriangle(0, i - 1, i);
            }
        }
        vh.AddTriangle(0, VertexNum, 1);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        GenerateSimpleSprite(vh, false);
    }
}
