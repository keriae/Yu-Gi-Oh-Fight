using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class DuelLinksCharacterBuilder : MonoBehaviour
{
    private void Start()
    {
        _ = AsyncStart();
    }

    private async UniTask AsyncStart()
    {
        string input = @"D:\Users\Desktop\new\characters\";
        string output = @"D:\Users\Desktop\new\characters output\";
        var results = await ProcessSnFolders(input, output);
        foreach (var kv in results)
            if(!kv.Value)
                Debug.Log($"sn{kv.Key}: Failed.");
        Debug.Log("End");
    }


    /// <summary>
    /// 处理所有 sn 文件夹，合并 Body 和 Face 图片并保存。
    /// </summary>
    /// <param name="inputFolder">输入文件夹路径（绝对路径）</param>
    /// <param name="outputFolder">输出文件夹路径（绝对路径）</param>
    /// <returns>字典，Key 为 sn 后面的数字字符串，Value 为处理是否成功（Body 和 Face 都存在且合并成功）</returns>
    public static async UniTask<Dictionary<string, bool>> ProcessSnFolders(string inputFolder, string outputFolder)
    {
        var resultDict = new Dictionary<string, bool>();

        if (!Directory.Exists(inputFolder))
        {
            Debug.LogError($"输入文件夹不存在: {inputFolder}");
            return resultDict;
        }

        // 创建输出文件夹
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        // 匹配 "sn" 后跟一个或多个数字的文件夹名
        Regex snRegex = new(@"^sn(\d+)$", RegexOptions.IgnoreCase);

        // 获取所有子文件夹
        string[] subDirs = Directory.GetDirectories(inputFolder);
        foreach (string dirPath in subDirs)
        {
            string dirName = Path.GetFileName(dirPath);
            Match match = snRegex.Match(dirName);
            if (!match.Success) continue;

            string snNumber = match.Groups[1].Value;  // 数字部分
            if( snNumber == "9005")
                snNumber = "9995"; // 死亡导游
            bool success = true;

            // 1. 查找 Body 图片
            string bodyEnUs = Path.Combine(dirPath, "images", "en-us", "Chara007_0.png");
            string bodyJaJp = Path.Combine(dirPath, "images", "ja-jp", "Chara007_0.png");
            string bodyPath = File.Exists(bodyEnUs) ? bodyEnUs : (File.Exists(bodyJaJp) ? bodyJaJp : null);

            // 2. 查找 Face 图片
            string faceEnUs = Path.Combine(dirPath, "images", "en-us", "Chara007_1.png");
            string faceJaJp = Path.Combine(dirPath, "images", "ja-jp", "Chara007_1.png");
            string facePath = File.Exists(faceEnUs) ? faceEnUs : (File.Exists(faceJaJp) ? faceJaJp : null);

            if (string.IsNullOrEmpty(bodyPath))
            {
                Debug.LogWarning($"sn{snNumber}: Body 缺失");
                resultDict[snNumber] = false;
                continue;
            }

            string outputFileName = $"sn{snNumber}_2.png";
            string outputPath = Path.Combine(outputFolder, outputFileName);

            // 3. 加载图片
            Texture2D bodyTex = LoadTextureFromFile(bodyPath);
            if (string.IsNullOrEmpty(facePath) && bodyTex != null)
            {
                if (bodyTex.width != 1024 || bodyTex.height != 1024)
                    Debug.LogWarning($"sn{snNumber}: Body 图片尺寸不是 1024x1024，实际为 {bodyTex.width}x{bodyTex.height}");
                SaveTextureAsPNG(bodyTex, outputPath);
                resultDict[snNumber] = true;
                await UniTask.Yield();
                continue;
            }
            Texture2D faceTex = LoadTextureFromFile(facePath);

            if (bodyTex == null || faceTex == null)
            {
                Debug.LogError($"sn{snNumber}: 图片加载失败 (Body: {bodyPath}, Face: {faceEnUs})");
                resultDict[snNumber] = false;
                continue;
            }

            if (bodyTex.width != 1024 || bodyTex.height != 1024)
                Debug.LogWarning($"sn{snNumber}: Body 图片尺寸不是 1024x1024，实际为 {bodyTex.width}x{bodyTex.height}");
            if (faceTex.width != 240 || faceTex.height != 240)
                Debug.LogWarning($"sn{snNumber}: Face 图片尺寸不是 240x240，实际为 {faceTex.width}x{faceTex.height}");

            // 4. 合并图片：将 Face 覆盖到 Body 中心点正上方 160 像素处
            Texture2D mergedTex = MergeImages(bodyTex, faceTex, out bool mergeSuccess);
            if (!mergeSuccess)
            {
                Debug.LogError($"sn{snNumber}: 图片合并失败");
                resultDict[snNumber] = false;
                continue;
            }

            // 5. 保存结果
            SaveTextureAsPNG(mergedTex, outputPath);
            //Debug.Log($"sn{snNumber}: 成功生成 {outputPath}");

            resultDict[snNumber] = true;
            await UniTask.Yield();
        }

        return resultDict;
    }

    /// <summary>
    /// 从文件路径加载 Texture2D (非 AssetDatabase，支持任意路径)
    /// </summary>
    private static Texture2D LoadTextureFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(fileData))
            return tex;
        else
        {
            Object.Destroy(tex);
            return null;
        }
    }

    /// <summary>
    /// 将 face 图片合并到 body 图片的中心正上方 160 像素处。
    /// 使用 Graphics.Blit + 自定义 Shader 实现精确位置覆盖。
    /// </summary>
    /// <param name="body">背景图片 (1024x1024)</param>
    /// <param name="face">前景图片 (240x240)</param>
    /// <param name="success">是否成功</param>
    /// <returns>合并后的 Texture2D</returns>
    private static Texture2D MergeImages(Texture2D body, Texture2D face, out bool success)
    {
        success = false;
        if (body == null || face == null) return null;

        int bodyW = body.width;
        int bodyH = body.height;
        int faceW = face.width;
        int faceH = face.height;

        // 计算 Face 在 Body 上的目标区域 (中心点正上方 160px)
        float centerX = bodyW * 0.5f;
        float centerY = bodyH * 0.5f + 160f;        // 正上方 +160
        float left = centerX - faceW * 0.5f;
        float bottom = centerY - faceH * 0.5f;

        // 边界检查 (理论上 1024x1024 完全容得下 240x240 偏移 160)
        if (left < 0 || left + faceW > bodyW || bottom < 0 || bottom + faceH > bodyH)
        {
            Debug.LogError($"Face 区域超出 Body 边界: left={left}, bottom={bottom}, bodySize={bodyW}x{bodyH}");
            return null;
        }

        // 计算目标区域在 Body 纹理中的 UV 范围
        Rect uvRect = new Rect(
            left / bodyW,
            bottom / bodyH,
            faceW / (float)bodyW,
            faceH / (float)bodyH
        );

        // 创建临时的 RenderTexture
        RenderTexture rt = RenderTexture.GetTemporary(bodyW, bodyH, 0, RenderTextureFormat.ARGB32);
        Graphics.SetRenderTarget(rt);
        GL.Clear(true, true, Color.clear);

        // 创建合并用的材质 (使用自定义 Shader)
        Material mergeMat = CreateMergeMaterial();
        if (mergeMat == null)
        {
            Debug.LogError("无法创建合并材质，请确保支持自定义 Shader");
            RenderTexture.ReleaseTemporary(rt);
            return null;
        }

        mergeMat.SetTexture("_OverlayTex", face);
        mergeMat.SetVector("_OverlayUVRect", new Vector4(uvRect.x, uvRect.y, uvRect.width, uvRect.height));

        // Graphics.Blit: 源 body，目标 rt，使用材质处理
        Graphics.Blit(body, rt, mergeMat);

        // 从 RenderTexture 读回 Texture2D
        Texture2D result = new Texture2D(bodyW, bodyH, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        result.ReadPixels(new Rect(0, 0, bodyW, bodyH), 0, 0);
        result.Apply();

        // 清理
        RenderTexture.active = null;
        Graphics.SetRenderTarget(null);
        RenderTexture.ReleaseTemporary(rt);
        Object.Destroy(mergeMat);

        success = true;
        return result;
    }

    /// <summary>
    /// 创建用于合并图片的自定义材质，Shader 可以动态生成或从内置资源加载。
    /// </summary>
    private static Material CreateMergeMaterial()
    {

        Shader shader = Shader.Find("Hidden/ImageMerger");
        Material mat = new(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        return mat;
    }

    /// <summary>
    /// 将 Texture2D 保存为 PNG 文件
    /// </summary>
    private static void SaveTextureAsPNG(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Object.Destroy(tex); // 释放内存
    } 
}