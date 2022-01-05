using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fileExplore.FileInfoBuilder
{
    interface IFileInfoBuilder
    {
        InfoBuilder AddName(string name);
        InfoBuilder AddContent(string content);
        InfoBuilder AddPath(string path);
        fileInfo Build();
    }
}
 