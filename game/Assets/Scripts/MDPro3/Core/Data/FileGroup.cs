
using MDPro3.IO;
using System.Collections.Generic;

namespace MDPro3.Data
{
    public class FileGroup
    {
        /// <summary>
        /// 注释
        /// </summary>
        public string Note;
        /// <summary>
        /// 以/结尾，表示目录。否则表示文件路径。
        /// </summary>
        public string[] Paths;
        /// <summary>
        /// 文件扩展名，表示目录中应索引的文件类型。
        /// </summary>
        public string[] Extensions;

        public bool FileExists(string filename)
        {
            foreach (var p in Paths)
                if (p.EndsWith("/"))
                    foreach (var ext in Extensions)
                    {
                        var name = System.IO.Path.Combine(p, filename + ext);
                        if(FileManager.FileExists(name))
                            return true;
                    }
            return false;
        }
    }

    public class CardFileGroup : FileGroup
    {
        public bool CardExists(int code)
        {
            return FileExists(code.ToString());
        }

        public string GetCardPath(int code)
        {
            foreach (var p in Paths)
                if (p.EndsWith("/"))
                    foreach (var ext in Extensions)
                    {
                        var name = System.IO.Path.Combine(p, code + ext);
                        if (FileManager.FileExists(name))
                            return name;
                    }
            return null;
        }
    }
}