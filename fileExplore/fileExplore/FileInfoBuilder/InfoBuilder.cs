using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileExplore.FileInfoBuilder
{
    class InfoBuilder : IFileInfoBuilder
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }

        public InfoBuilder AddContent(string content)
        {
            Content = content;
            return this;
        }

        public InfoBuilder AddName(string name)
        {
            Name = name;
            return this;
        }

        public InfoBuilder AddPath(string path)
        {
            Path = path;
            return this;
        }

        public fileInfo Build()
        {
            return new fileInfo(Name, Content, Path);
        }
    }
}
