using System.Collections.Generic;

namespace MDPro3.Data
{
    public class FileGroupConfig
    {
        public static readonly string SavePath = Program.PATH_DATA + "FileGroups.json";

        public string[] zipExtensions = new string[] { ".zip", ".ypk" };
        public void Save()
        {
            Json.SaveToFile(SavePath, this);
        }

        public bool IsZipFile(string path)
        {
            if(zipExtensions == null || zipExtensions.Length == 0)
                return false;
            foreach (var ext in zipExtensions)
            {
                if (path.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public FileGroup Expansions = new()
        {
            Note = "扩展文件等Zip文件。",
            Paths = new string[] { "Expansions/" },
            Extensions = new string[] { ".zip", ".ypk" },
        };

        public FileGroup ScriptZip = new()
        {
            Note = "默认lua脚本文件zip。",
            Paths = new string[] { "Data/script.zip" },
            Extensions = new string[] { ".zip" },
        };

        public FileGroup CardScript = new()
        {
            Note = "卡片脚本。",
            Paths = new string[] { "script/" },
            Extensions = new string[] { ".lua" },
        };

        public CardFileGroup CardIllust = new()
        {
            Note = "卡片插画。",
            Paths = new string[] { "Picture/Art/", "art/" },
            Extensions = new string[] { ".jpg" },
        };

        public CardFileGroup CardIllust2 = new()
        {
            Note = "卡片插画，对于超框卡是框内插画，对于非超框卡是替换默认卡牌插画的快捷方式。",
            Paths = new string[] { "Picture/Art2/", "art2/" },
            Extensions = new string[] { ".png" },
        };

        public CardFileGroup CardCloseup = new()
        {
            Note = "怪兽立绘，决斗场地中怪兽卡上方的立绘。",
            Paths = new string[] { "Picture/Closeup/", "closeup/" },
            Extensions = new string[] { ".png" },
        };

        public CardFileGroup CardOverframe = new()
        {
            Note = "超框卡卡图，jpg格式视为完整超框卡图。",
            Paths = new string[] { "Picture/OverFrame/", "overframe/" },
            Extensions = new string[] { ".jpg" },
        };

        public CardFileGroup CardOverframe2 = new()
        {
            Note = "超框卡卡图，png格式视为透明卡图，512x1024的png视为MasterDuel释出的带透明通道的完整卡图。",
            Paths = new string[] { "Picture/OverFrame/", "overframe/" },
            Extensions = new string[] { ".png" },
        };

        public CardFileGroup CardPicture = new()
        {
            Note = "完整卡图，用于读取扩展卡包中的卡图。",
            Paths = new string[] { "pics/" },
            Extensions = new string[] { ".jpg" },
        };

        public CardFileGroup CardVideo = new()
        {
            Note = "动态卡图的视频文件。zip中的视频文件必须解压才能使用，默认使用Path中的第一个作为解压路径。因此Path[0]中已存在的文件不会被解压。",
            Paths = new string[] { "Video/Art/", "video/" },
            Extensions = new string[] { ".mp4" },
        };

        public List<FileGroup> GetList()
        {
            if (Config.GetBool("Expansions", true))
                return new List<FileGroup>()
                    { Expansions, ScriptZip, CardScript, CardIllust, CardIllust2, CardCloseup, CardOverframe, CardOverframe2, CardPicture, CardVideo };
            else
                return new List<FileGroup>()
                    { ScriptZip, CardScript, CardIllust, CardIllust2, CardCloseup, CardOverframe, CardOverframe2, CardPicture, CardVideo };
        }
    }
}


