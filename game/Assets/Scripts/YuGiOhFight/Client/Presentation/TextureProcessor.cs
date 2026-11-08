using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;


namespace YuGiOhFight.Client.Presentation
{
public static partial class TextureProcessor
{

    #region Common

    public enum BlendMode
    {
        Multiply,    // 相乘
        Replace,     // 替换
        Additive,    // 相加
        Subtractive, // 相减
        Overwrite    // 完全覆盖
    }

    private static UnityEngine.ComputeShader LoadComputeShader(string path)
    {
        UnityEngine.ComputeShader shader = UnityEngine.Resources.Load<UnityEngine.ComputeShader>(path);
        return shader;
    }

    private static void DispatchComputeShader(UnityEngine.ComputeShader shader, int kernel, UnityEngine.Texture sourceTex)
    {
        DispatchComputeShader(shader, kernel, sourceTex.width, sourceTex.height);
    }

    private static void DispatchComputeShader(UnityEngine.ComputeShader shader, int kernel, int width, int height)
    {
        shader.GetKernelThreadGroupSizes(kernel, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);

        int threadGroupsX = UnityEngine.Mathf.CeilToInt(width / (float)threadGroupSizeX);
        int threadGroupsY = UnityEngine.Mathf.CeilToInt(height / (float)threadGroupSizeY);

        shader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
    }

    private static UnityEngine.Texture2D RenderTextureToTexture2D(UnityEngine.RenderTexture rt, UnityEngine.Texture sizeReference)
    {
        UnityEngine.Texture2D resultTex = new(sizeReference.width, sizeReference.height, UnityEngine.TextureFormat.ARGB32, false);
        UnityEngine.RenderTexture.active = rt;
        resultTex.ReadPixels(new UnityEngine.Rect(0, 0, sizeReference.width, sizeReference.height), 0, 0);
        resultTex.Apply();
        UnityEngine.RenderTexture.active = null;
        return resultTex;
    }

    private static float GetChannelValue(UnityEngine.Color color, int channel)
    {
        return channel switch
        {
            // R
            0 => color.r,
            // G
            1 => color.g,
            // B
            2 => color.b,
            // A
            3 => color.a,
            _ => color.a,// 默认使用alpha通道
        };
    }

    private static UnityEngine.Color SampleTextureBilinear(UnityEngine.Color32[] pixels, int width, int height, float u, float v)
    {
        if (width == 0 || height == 0)
            return UnityEngine.Color.clear;

        // 计算双线性采样坐标
        float x = u * (width - 1);
        float y = v * (height - 1);

        int xFloor = UnityEngine.Mathf.FloorToInt(x);
        int yFloor = UnityEngine.Mathf.FloorToInt(y);
        int xCeil = UnityEngine.Mathf.Min(xFloor + 1, width - 1);
        int yCeil = UnityEngine.Mathf.Min(yFloor + 1, height - 1);

        // 获取四个采样点的颜色索引
        int index00 = yFloor * width + xFloor;
        int index10 = yFloor * width + xCeil;
        int index01 = yCeil * width + xFloor;
        int index11 = yCeil * width + xCeil;

        // 获取颜色（注意：数组可能越界，需要检查）
        UnityEngine.Color32 c00 = (index00 >= 0 && index00 < pixels.Length) ? pixels[index00] : new UnityEngine.Color32(0, 0, 0, 0);
        UnityEngine.Color32 c10 = (index10 >= 0 && index10 < pixels.Length) ? pixels[index10] : new UnityEngine.Color32(0, 0, 0, 0);
        UnityEngine.Color32 c01 = (index01 >= 0 && index01 < pixels.Length) ? pixels[index01] : new UnityEngine.Color32(0, 0, 0, 0);
        UnityEngine.Color32 c11 = (index11 >= 0 && index11 < pixels.Length) ? pixels[index11] : new UnityEngine.Color32(0, 0, 0, 0);

        // 计算权重
        float xWeight = x - xFloor;
        float yWeight = y - yFloor;

        // 将Color32转换为Color进行插值
        UnityEngine.Color color00 = c00;
        UnityEngine.Color color10 = c10;
        UnityEngine.Color color01 = c01;
        UnityEngine.Color color11 = c11;

        // 双线性插值
        UnityEngine.Color top = UnityEngine.Color.Lerp(color00, color10, xWeight);
        UnityEngine.Color bottom = UnityEngine.Color.Lerp(color01, color11, xWeight);
        return UnityEngine.Color.Lerp(top, bottom, yWeight);
    }

    private static float ApplyBlendMode(float sourceAlpha, float maskValue, BlendMode blendMode)
    {
        return blendMode switch
        {
            BlendMode.Multiply => sourceAlpha * maskValue,
            BlendMode.Replace => maskValue,
            BlendMode.Additive => UnityEngine.Mathf.Clamp01(sourceAlpha + maskValue),
            BlendMode.Subtractive => UnityEngine.Mathf.Clamp01(sourceAlpha - maskValue),
            BlendMode.Overwrite => sourceAlpha > 0 ? maskValue : 0,
            _ => sourceAlpha * maskValue,
        };
    }

    private static UnityEngine.Texture2D EnsureTextureReadable(UnityEngine.Texture2D texture)
    {
        if (texture == null) return null;
        if (texture.isReadable) return texture;

        UnityEngine.RenderTexture rt = UnityEngine.RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            UnityEngine.RenderTextureFormat.ARGB32
        );

        UnityEngine.RenderTexture previous = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active = rt;
        UnityEngine.Graphics.Blit(texture, rt);

        UnityEngine.Texture2D readableTex = new UnityEngine.Texture2D(
            texture.width,
            texture.height,
            UnityEngine.TextureFormat.ARGB32,
            false
        );

        readableTex.ReadPixels(new UnityEngine.Rect(0, 0, texture.width, texture.height), 0, 0);
        readableTex.Apply();

        UnityEngine.RenderTexture.active = previous;
        UnityEngine.RenderTexture.ReleaseTemporary(rt);

        return readableTex;
    }

    #endregion

    #region ApplyMaskToAlpha

    private const string PATH_APPLY_MASK_ALPHA = "ComputeShader/ApplyMaskAlpha";
    private static UnityEngine.ComputeShader shaderApplyMaskAlpha;
    private static UnityEngine.ComputeShader ShaderApplyMaskAlpha =>
        shaderApplyMaskAlpha != null ? shaderApplyMaskAlpha
        : shaderApplyMaskAlpha = LoadComputeShader(PATH_APPLY_MASK_ALPHA);

    public static UnityEngine.Texture2D ApplyMaskToAlpha(
        UnityEngine.Texture2D sourceTex,
        UnityEngine.Texture2D maskTex,
        BlendMode blendMode = BlendMode.Multiply,
        float threshold = 0.0f,
        bool invertMask = false,
        int maskChannel = 3
    )
    {
        // 1. 参数检查
        if (sourceTex == null || maskTex == null)
            throw new System.ArgumentNullException("Source and mask textures cannot be null.");

        // 2. 检查长宽比
        float sourceAspect = (float)sourceTex.width / sourceTex.height;
        float maskAspect = (float)maskTex.width / maskTex.height;

        if (UnityEngine.Mathf.Abs(sourceAspect - maskAspect) > 0.01f)
        {
            UnityEngine.Debug.LogWarning($"Aspect ratios don't match! Source: {sourceAspect:F3}, Mask: {maskAspect:F3}." +
                           " Results may be distorted.");
        }

        // 检查平台是否支持Compute Shader
        if (!UnityEngine.SystemInfo.supportsComputeShaders)
        {
            UnityEngine.Debug.LogWarning("Current platform does not support Compute Shaders. Falling back to CPU method.");
            return ApplyMaskToAlpha_CPU(sourceTex, maskTex, blendMode, threshold, invertMask, maskChannel);
        }

        // 3. 准备Compute Shader
        UnityEngine.ComputeShader shader = ShaderApplyMaskAlpha;
        int kernelHandle = shader.FindKernel("CSMain");

        // 4. 创建输出纹理
        UnityEngine.RenderTexture outputRT = RenderTexturePool.Get(sourceTex.width, sourceTex.height);

        // 5. 设置Compute Shader参数
        shader.SetTexture(kernelHandle, "SourceTex", sourceTex);
        shader.SetTexture(kernelHandle, "MaskTex", maskTex);
        shader.SetTexture(kernelHandle, "Result", outputRT);

        // 设置纹理尺寸
        shader.SetFloats("SourceSize", new float[] { sourceTex.width, sourceTex.height });
        shader.SetFloats("MaskSize", new float[] { maskTex.width, maskTex.height });

        // 设置混合模式和其他参数
        shader.SetInt("_BlendMode", (int)blendMode);
        shader.SetFloat("_Threshold", threshold);
        shader.SetFloat("_InvertMask", invertMask ? 1.0f : 0.0f);
        shader.SetInt("_MaskChannel", maskChannel);

        // 6. 调度线程组
        DispatchComputeShader(shader, kernelHandle, sourceTex);

        // 7. 转换回Texture2D
        UnityEngine.Texture2D resultTex = RenderTextureToTexture2D(outputRT, sourceTex);

        // 8. 清理
        RenderTexturePool.Return(outputRT);

        return resultTex;
    }

    public static UnityEngine.Texture2D ApplyMaskToAlpha_CPU(
        UnityEngine.Texture2D sourceTex,
        UnityEngine.Texture2D maskTex,
        BlendMode blendMode = BlendMode.Multiply,
        float threshold = 0.0f,
        bool invertMask = false,
        int maskChannel = 3
    )
    {
        // 确保纹理可读写
        UnityEngine.Texture2D sourceReadable = EnsureTextureReadable(sourceTex);
        UnityEngine.Texture2D maskReadable = EnsureTextureReadable(maskTex);

        // 使用32位颜色数组
        UnityEngine.Color32[] sourcePixels = sourceReadable.GetPixels32();
        UnityEngine.Color32[] maskPixels = maskReadable.GetPixels32();

        int sourceWidth = sourceReadable.width;
        int sourceHeight = sourceReadable.height;
        int maskWidth = maskReadable.width;
        int maskHeight = maskReadable.height;
        bool sameSize = (sourceWidth == maskWidth) && (sourceHeight == maskHeight);

        // 创建结果像素数组
        UnityEngine.Color32[] resultPixels = new UnityEngine.Color32[sourcePixels.Length];

        // 使用并行处理提高性能
        System.Threading.Tasks.Parallel.For(0, sourceHeight, y =>
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                int sourceIndex = y * sourceWidth + x;
                UnityEngine.Color32 sourceColor = sourcePixels[sourceIndex];
                UnityEngine.Color maskColor;

                if (sameSize)
                {
                    // 尺寸完全一致时，直接通过相同坐标访问，无需UV映射和采样
                    int maskIndex = y * maskWidth + x;
                    maskColor = maskPixels[maskIndex];
                }
                else
                {
                    // 尺寸不一致时，计算对应的mask UV坐标
                    float u = sourceWidth > 1 ? x / (float)(sourceWidth - 1) : 0.5f;
                    float v = sourceHeight > 1 ? y / (float)(sourceHeight - 1) : 0.5f;

                    // 获取mask颜色（使用双线性采样）
                    maskColor = SampleTextureBilinear(maskPixels, maskWidth, maskHeight, u, v);
                }

                // 根据指定通道获取mask值
                float maskValue = GetChannelValue(maskColor, maskChannel);

                // 应用阈值
                if (threshold > 0)
                {
                    maskValue = maskValue >= threshold ? 1.0f : 0.0f;
                }

                // 反转mask
                if (invertMask)
                {
                    maskValue = 1.0f - maskValue;
                }

                // 应用混合模式
                float sourceAlpha = sourceColor.a / 255f; // 转换为0-1范围
                float newAlphaFloat = ApplyBlendMode(sourceAlpha, maskValue, blendMode);

                // 转换为byte
                byte newAlpha = (byte)UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(newAlphaFloat * 255), 0, 255);

                // 保持原RGB，只修改Alpha
                resultPixels[sourceIndex] = new UnityEngine.Color32(
                    sourceColor.r,
                    sourceColor.g,
                    sourceColor.b,
                    newAlpha
                );
            }
        });

        UnityEngine.Texture2D result = new(sourceWidth, sourceHeight, UnityEngine.TextureFormat.ARGB32, false);
        result.SetPixels32(resultPixels);
        result.Apply();

        // 清理临时纹理
        CleanupTemporaryTextures(sourceReadable, sourceTex, maskReadable, maskTex);

        return result;
    }

    private static void CleanupTemporaryTextures(
        UnityEngine.Texture2D temp1, UnityEngine.Texture2D original1,
        UnityEngine.Texture2D temp2, UnityEngine.Texture2D original2
    )
    {
        if (temp1 != null && temp1 != original1)
        {
            UnityEngine.Object.Destroy(temp1);
        }

        if (temp2 != null && temp2 != original2)
        {
            UnityEngine.Object.Destroy(temp2);
        }
    }

    #endregion

    #region InvertAlpha

    private const string PATH_INVERT_ALPHA = "ComputeShader/InvertAlpha";
    private static UnityEngine.ComputeShader shaderInvertMaskAlpha;
    private static UnityEngine.ComputeShader ShaderInvertMaskAlpha =>
        shaderInvertMaskAlpha != null ? shaderInvertMaskAlpha
        : shaderInvertMaskAlpha = LoadComputeShader(PATH_INVERT_ALPHA);

    public static UnityEngine.Texture2D InvertAlpha(UnityEngine.Texture2D texture)
    {
        if(texture == null)
            throw new System.ArgumentNullException("Texture cannot be null.");
        if (!UnityEngine.SystemInfo.supportsComputeShaders)
        {
            UnityEngine.Debug.LogWarning("Current platform does not support Compute Shaders. Falling back to CPU method.");
            return InvertAlpha_CPU(texture);
        }

        UnityEngine.ComputeShader shader = ShaderInvertMaskAlpha;
        int kernelHandle = shader.FindKernel("CSMain");
        UnityEngine.RenderTexture outputRT = RenderTexturePool.Get(texture.width, texture.height);
        shader.SetTexture(kernelHandle, "SourceTex", texture);
        shader.SetTexture(kernelHandle, "Result", outputRT);
        DispatchComputeShader(shader, kernelHandle, texture);
        UnityEngine.Texture2D resultTex = RenderTextureToTexture2D(outputRT, texture);
        RenderTexturePool.Return(outputRT);
        return resultTex;
    }

    public static UnityEngine.Texture2D InvertAlpha_CPU(UnityEngine.Texture2D texture)
    {
        if (texture == null)
        {
            UnityEngine.Debug.LogError("Source Texture is null!");
            return null;
        }

        int width = texture.width;
        int height = texture.height;

        // 创建目标纹理
        UnityEngine.Texture2D resultTexture = new(width, height, UnityEngine.TextureFormat.ARGB32, false);

        // 使用GetPixels32获取像素数据
        UnityEngine.Color32[] pixels = texture.GetPixels32();

        // 使用Parallel.For进行并行处理（提高性能）
        System.Threading.Tasks.Parallel.For(0, pixels.Length, i =>
        {
            UnityEngine.Color32 original = pixels[i];
            // 反转alpha值，保持RGB不变
            pixels[i] = new UnityEngine.Color32(original.r, original.g, original.b,
                                  (byte)(255 - original.a));
        });

        // 应用修改到纹理
        resultTexture.SetPixels32(pixels);
        resultTexture.Apply();
        return resultTexture;
    }

    #endregion

    #region Texture Resize

    private const string PATH_TEXTURE_RESIZE = "ComputeShader/TextureResize";
    private static UnityEngine.ComputeShader shaderTextureResize;
    private static UnityEngine.ComputeShader ShaderTextureResize =>
        shaderTextureResize != null ? shaderTextureResize
        : shaderTextureResize = LoadComputeShader(PATH_TEXTURE_RESIZE);

    public static UnityEngine.Texture2D ResizeTexture2D(UnityEngine.Texture2D texture, int newWidth, int newHeight)
    {
        if (texture == null)
            throw new System.ArgumentNullException("Texture cannot be null.");
        if (!UnityEngine.SystemInfo.supportsComputeShaders)
        {
            UnityEngine.Debug.LogWarning("Current platform does not support Compute Shaders. Falling back to CPU method.");
            return ResizeTexture2D_CPU(texture, newWidth, newHeight);
        }

        UnityEngine.ComputeShader shader = ShaderTextureResize;
        int kernelHandle = shader.FindKernel("CSMain");
        UnityEngine.RenderTexture outputRT = RenderTexturePool.Get(newWidth, newHeight);

        shader.SetTexture(kernelHandle, "Result", outputRT);
        shader.SetTexture(kernelHandle, "Source", texture);
        shader.SetInt("SourceWidth", texture.width);
        shader.SetInt("SourceHeight", texture.height);
        shader.SetInt("DestWidth", newWidth);
        shader.SetInt("DestHeight", newHeight);
        DispatchComputeShader(shader, kernelHandle, outputRT);
        UnityEngine.Texture2D resultTex = RenderTextureToTexture2D(outputRT, outputRT);
        RenderTexturePool.Return(outputRT);
        return resultTex;
    }

    public static UnityEngine.Texture2D ResizeTexture2D_CPU(UnityEngine.Texture2D texture, int newWidth, int newHeight)
    {
        UnityEngine.Color32[] srcPixels = texture.GetPixels32();
        UnityEngine.Color32[] dstPixels = ResizePixelsBilinear32(srcPixels, texture.width, texture.height, newWidth, newHeight);
        UnityEngine.Texture2D dst = new(newWidth, newHeight, texture.format, false);
        dst.SetPixels32(dstPixels);
        dst.Apply();

        return dst;
    }

    private static UnityEngine.Color32[] ResizePixelsBilinear32(UnityEngine.Color32[] srcPixels, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        UnityEngine.Color32[] dstPixels = new UnityEngine.Color32[dstWidth * dstHeight];

        for (int y = 0; y < dstHeight; y++)
        {
            for (int x = 0; x < dstWidth; x++)
            {
                float u = (x + 0.5f) / dstWidth;
                float v = (y + 0.5f) / dstHeight;
                UnityEngine.Color sampledColor = SampleTextureBilinear(srcPixels, srcWidth, srcHeight, u, v);
                dstPixels[y * dstWidth + x] = sampledColor;
            }
        }

        return dstPixels;
    }

    #endregion

    #region PadBottomToMatchAspect

    public static UnityEngine.Texture2D PadBottomToMatchAspect(UnityEngine.Texture2D source, float targetAspect)
    {
        if (source == null) return null;

        int width = source.width;
        int height = source.height;
        float currentAspect = (float)width / height;

        if (currentAspect <= targetAspect)
            return source;

        int newHeight = UnityEngine.Mathf.CeilToInt(width / targetAspect);

        UnityEngine.Texture2D result = new(width, newHeight, source.format, false);

        UnityEngine.Color32[] transparentPixels = new UnityEngine.Color32[width * newHeight];
        for (int i = 0; i < transparentPixels.Length; i++)
            transparentPixels[i] = new UnityEngine.Color32(0, 0, 0, 0);
        result.SetPixels32(transparentPixels);
        result.Apply();

        int dstY = newHeight - height;
        UnityEngine.Graphics.CopyTexture(source, 0, 0, 0, 0, width, height,
                             result, 0, 0, 0, dstY);

        return result;
    }

    #endregion

    #region Crop Texture

    public static UnityEngine.Texture2D GetCroppingTex(UnityEngine.Texture2D texture, int startX, int startY, int width, int height)
    {
        int destWidth = width - startX;
        int destHeight = height - startY;

        var result = new UnityEngine.Texture2D(destWidth, destHeight, texture.format, false);
        UnityEngine.Graphics.CopyTexture(texture, 0, 0, startX, startY, destWidth, destHeight,
                             result, 0, 0, 0, 0);

        return result;
    }

    #endregion

    #region Check Texture Opaque

    private const string PATH_OPAQUE_CHECKER = "ComputeShader/OpaqueChecker";
    private static UnityEngine.ComputeShader shaderOpaqueChecker;
    private static UnityEngine.ComputeShader ShaderOpaqueChecker =>
        shaderOpaqueChecker != null ? shaderOpaqueChecker
        : shaderOpaqueChecker = LoadComputeShader(PATH_OPAQUE_CHECKER);

    private static readonly int _ResultId = UnityEngine.Shader.PropertyToID("_Result");
    private static readonly int _MainTexId = UnityEngine.Shader.PropertyToID("_MainTex");
    private static readonly int _WidthId = UnityEngine.Shader.PropertyToID("_Width");
    private static readonly int _HeightId = UnityEngine.Shader.PropertyToID("_Height");
    private static readonly int _AlphaThresholdId = UnityEngine.Shader.PropertyToID("_AlphaThreshold");

    public static bool IsFullyOpaque(UnityEngine.Texture2D texture, byte threshold = 254)
    {
        if (texture == null)
            throw new System.ArgumentNullException(nameof(texture));

        if (UnityEngine.SystemInfo.supportsComputeShaders)
        {
            try
            {
                return IsFullyOpaque_GPU(texture, threshold);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"GPU check failed, fallback to CPU. Error: {e.Message}");
                return IsFullyOpaque_CPU(texture, threshold);
            }
        }

        return IsFullyOpaque_CPU(texture, threshold);
    }

    private static bool IsFullyOpaque_GPU(UnityEngine.Texture2D texture, byte threshold)
    {
        UnityEngine.ComputeShader shader = ShaderOpaqueChecker;

        UnityEngine.ComputeBuffer resultBuffer = new(1, sizeof(int), UnityEngine.ComputeBufferType.Raw);
        resultBuffer.SetData(new int[] { 0 });

        shader.SetBuffer(0, _ResultId, resultBuffer);
        shader.SetTexture(0, _MainTexId, texture);
        shader.SetInt(_WidthId, texture.width);
        shader.SetInt(_HeightId, texture.height);
        shader.SetInt(_AlphaThresholdId, threshold);

        int kernelHandle = shader.FindKernel("CSMain");
        DispatchComputeShader(shader, kernelHandle, texture);

        int[] data = new int[1];
        resultBuffer.GetData(data);
        resultBuffer.Release();

        return data[0] == 0; // 0 = 全不透明，1 = 存在透明像素
    }

    private static bool IsFullyOpaque_CPU(UnityEngine.Texture2D texture, byte threshold)
    {
        if (!texture.isReadable)
            throw new System.ArgumentException("CPU fallback requires texture Read/Write enabled.", nameof(texture));

        // 无 Alpha 通道的格式默认全部不透明
        if (!HasAlphaChannel(texture.format))
            return true;

        // 仅支持每像素 4 字节的 RGBA 类格式
        if (!TryGetPixelInfo(texture.format, out int bytesPerPixel, out int alphaOffset))
        {
            UnityEngine.Debug.LogWarning($"Unsupported texture format for CPU: {texture.format}. Converting to RGBA32.");
            var converted = ConvertToRGBA32(texture);
            bool result = IsFullyOpaque_CPU(converted, threshold);
            UnityEngine.Object.DestroyImmediate(converted);
            return result;
        }

        NativeArray<byte> pixelData = texture.GetPixelData<byte>(0);
        int pixelCount = texture.width * texture.height;

        // 根据像素数量选择单线程或 Job 并行
        if (pixelCount > 65536) // 大于 256x256
        {
            return CheckWithJobs(pixelData, bytesPerPixel, alphaOffset, threshold, pixelCount);
        }
        else
        {
            return CheckWithSpan(pixelData, bytesPerPixel, alphaOffset, threshold, pixelCount);
        }
    }

    private static unsafe bool CheckWithSpan(NativeArray<byte> data, int bytesPerPixel, int alphaOffset, byte threshold, int pixelCount)
    {
        var span = new System.Span<byte>(data.GetUnsafePtr(), data.Length);
        for (int i = 0; i < pixelCount; i++)
        {
            if (span[i * bytesPerPixel + alphaOffset] < threshold)
                return false;
        }
        return true;
    }

    private static bool CheckWithJobs(NativeArray<byte> data, int bytesPerPixel, int alphaOffset, byte threshold, int pixelCount)
    {
        var result = new NativeArray<bool>(1, Allocator.TempJob);
        result[0] = true;

        var job = new OpaqueCheckJob
        {
            Data = data,
            BytesPerPixel = bytesPerPixel,
            AlphaOffset = alphaOffset,
            AlphaThreshold = threshold,
            Result = result
        };

        JobHandle handle = job.Schedule(pixelCount, 1024);
        handle.Complete();

        bool isOpaque = result[0];
        result.Dispose();
        return isOpaque;
    }

    [BurstCompile]
    private struct OpaqueCheckJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<byte> Data;
        public int BytesPerPixel;
        public int AlphaOffset;
        public byte AlphaThreshold;
        public NativeArray<bool> Result;

        public void Execute(int index)
        {
            if (!Result[0]) return;
            int alphaIndex = index * BytesPerPixel + AlphaOffset;
            if (Data[alphaIndex] < AlphaThreshold)
                Result[0] = false;
        }
    }

    private static bool HasAlphaChannel(UnityEngine.TextureFormat format)
    {
        switch (format)
        {
            case UnityEngine.TextureFormat.Alpha8:
            case UnityEngine.TextureFormat.ARGB32:
            case UnityEngine.TextureFormat.RGBA32:
            case UnityEngine.TextureFormat.BGRA32:
            case UnityEngine.TextureFormat.RGBAFloat:
            case UnityEngine.TextureFormat.RGBAHalf:
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetPixelInfo(UnityEngine.TextureFormat format, out int bytesPerPixel, out int alphaOffset)
    {
        bytesPerPixel = 0;
        alphaOffset = -1;

        switch (format)
        {
            case UnityEngine.TextureFormat.RGBA32:
                bytesPerPixel = 4;
                alphaOffset = 3;
                return true;
            case UnityEngine.TextureFormat.ARGB32:
                bytesPerPixel = 4;
                alphaOffset = 0;
                return true;
            case UnityEngine.TextureFormat.BGRA32:
                bytesPerPixel = 4;
                alphaOffset = 3;
                return true;
            default:
                return false;
        }
    }

    private static UnityEngine.Texture2D ConvertToRGBA32(UnityEngine.Texture2D source)
    {
        UnityEngine.RenderTexture tmp = UnityEngine.RenderTexture.GetTemporary(source.width, source.height, 0, UnityEngine.RenderTextureFormat.ARGB32);
        UnityEngine.Graphics.Blit(source, tmp);
        UnityEngine.RenderTexture previous = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active = tmp;
        var converted = new UnityEngine.Texture2D(source.width, source.height, UnityEngine.TextureFormat.RGBA32, false);
        converted.ReadPixels(new UnityEngine.Rect(0, 0, source.width, source.height), 0, 0);
        converted.Apply();
        UnityEngine.RenderTexture.active = previous;
        UnityEngine.RenderTexture.ReleaseTemporary(tmp);
        return converted;
    }

    #endregion

    #region BlackenAlpha

    private const string PATH_BLACKEN_ALPHA = "ComputeShader/BlackenAlpha";
    private static UnityEngine.ComputeShader shaderBlackenAlpha;
    private static UnityEngine.ComputeShader ShaderBlackenAlpha =>
        shaderBlackenAlpha != null ? shaderBlackenAlpha
        : shaderBlackenAlpha = LoadComputeShader(PATH_BLACKEN_ALPHA);

    public static UnityEngine.Texture2D BlackenAlpha(UnityEngine.Texture2D texture)
    {
        if (texture == null)
            throw new System.ArgumentNullException("Texture cannot be null.");
        if (!UnityEngine.SystemInfo.supportsComputeShaders)
        {
            UnityEngine.Debug.LogWarning("Current platform does not support Compute Shaders. Falling back to CPU method.");
            return BlackenAlpha_CPU(texture);
        }

        UnityEngine.ComputeShader shader = ShaderBlackenAlpha;
        int kernelHandle = shader.FindKernel("CSMain");
        UnityEngine.RenderTexture outputRT = RenderTexturePool.Get(texture.width, texture.height);
        shader.SetTexture(kernelHandle, "SourceTex", texture);
        shader.SetTexture(kernelHandle, "Result", outputRT);
        DispatchComputeShader(shader, kernelHandle, texture);
        UnityEngine.Texture2D resultTex = RenderTextureToTexture2D(outputRT, texture);
        RenderTexturePool.Return(outputRT);
        return resultTex;
    }

    public static UnityEngine.Texture2D BlackenAlpha_CPU(UnityEngine.Texture2D texture)
    {
        if (texture == null)
        {
            UnityEngine.Debug.LogError("Source Texture is null!");
            return null;
        }

        int width = texture.width;
        int height = texture.height;
        UnityEngine.Texture2D resultTexture = new(width, height, UnityEngine.TextureFormat.ARGB32, false);
        UnityEngine.Color32[] pixels = texture.GetPixels32();

        System.Threading.Tasks.Parallel.For(0, pixels.Length, i =>
        {
            UnityEngine.Color32 original = pixels[i];
            pixels[i] = new UnityEngine.Color32((byte)(255 - original.a), (byte)(255 - original.a), (byte)(255 - original.a), 255);
        });

        resultTexture.SetPixels32(pixels);
        resultTexture.Apply();
        return resultTexture;
    }

    #endregion

    #region SetAlphaToOne

    private const string PATH_SET_ALPHA_TO_ONE = "ComputeShader/SetAlphaToOne";
    private static UnityEngine.ComputeShader shaderSetAlphaToOne;
    private static UnityEngine.ComputeShader ShaderSetAlphaToOne =>
        shaderSetAlphaToOne != null ? shaderSetAlphaToOne
        : shaderSetAlphaToOne = LoadComputeShader(PATH_SET_ALPHA_TO_ONE);

    public static UnityEngine.Texture2D SetAlphaToOne(UnityEngine.Texture2D texture)
    {
        if (texture == null)
            throw new System.ArgumentNullException("Texture cannot be null.");
        if (!UnityEngine.SystemInfo.supportsComputeShaders)
        {
            UnityEngine.Debug.LogWarning("Current platform does not support Compute Shaders. Falling back to CPU method.");
            return SetAlphaToOne_CPU(texture);
        }

        UnityEngine.ComputeShader shader = ShaderSetAlphaToOne;
        int kernelHandle = shader.FindKernel("CSMain");
        UnityEngine.RenderTexture outputRT = RenderTexturePool.Get(texture.width, texture.height);
        shader.SetTexture(kernelHandle, "SourceTex", texture);
        shader.SetTexture(kernelHandle, "Result", outputRT);
        DispatchComputeShader(shader, kernelHandle, texture);
        UnityEngine.Texture2D resultTex = RenderTextureToTexture2D(outputRT, texture);
        RenderTexturePool.Return(outputRT);
        return resultTex;
    }

    public static UnityEngine.Texture2D SetAlphaToOne_CPU(UnityEngine.Texture2D texture)
    {
        if (texture == null)
        {
            UnityEngine.Debug.LogError("Source Texture is null!");
            return null;
        }

        int width = texture.width;
        int height = texture.height;
        UnityEngine.Texture2D resultTexture = new(width, height, UnityEngine.TextureFormat.ARGB32, false);
        UnityEngine.Color32[] pixels = texture.GetPixels32();

        System.Threading.Tasks.Parallel.For(0, pixels.Length, i =>
        {
            UnityEngine.Color32 original = pixels[i];
            // 只修改 Alpha，RGB 不变
            pixels[i] = new UnityEngine.Color32(original.r, original.g, original.b, 255);
        });

        resultTexture.SetPixels32(pixels);
        resultTexture.Apply();
        return resultTexture;
    }

    #endregion

}
}
