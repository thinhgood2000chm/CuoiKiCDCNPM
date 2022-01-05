using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileExplore.FileInfoBuilder
{
    class fileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }

        public fileInfo(string name,  string content, string path)
        {
            Name = name;
            Content = content;
            Path = path;
        }
    }
}
