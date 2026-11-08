# 发行稀有度角标

使用项目 `.text2image.md` 中的 Tika 工作流，以 `gpt-image-2` 根据用户提供的卡池参考图生成 `atlas.png`。完整提示词保存在 `prompt.txt`。

N / R 为银色金属，SR 为香槟金，UR / SE 逐级增加金边、高光和金色星芒。罕见发行代码使用同一 AI 图集衍生的空白徽标，由客户端绘制准确文字；更高等级使用更明亮的金色星芒边框。

运行 `python3 tools/rarity-art/extract_atlas.py` 重建 `content/client-ui/rarity/*.png`。脚本只移除与外部相连的黑色背景，保留深色金属内衬，并统一输出为透明 192 × 96 PNG。客户端显示尺寸为 20 × 10。
