using System;
using Nest;
using System.Collections.Generic;
using System.Diagnostics;
using fileExplore.FileInfoBuilder;

namespace fileExplore.Dao
{
    class fileDao
    {
        static ConnectionSettings connectionSettings = new ConnectionSettings(new Uri("http://localhost:9200/")); //local PC      
        ElasticClient elasticClient = new ElasticClient(connectionSettings);
        public bool AddList(List<fileInfo> fileInfos, string index)
        {
            var bulkIndexResponse = elasticClient.Bulk(b => b
               .Index(index)
               .IndexMany(fileInfos)
             );

            if (bulkIndexResponse.IsValid)
            {
                return true;
            }
            return false;
        }

        public bool Add(fileInfo file, string index)
        {
            var myJson = new
            {
                name = file.Name,
                path = file.Path,
                content = file.Content
            };
            var response = elasticClient.Index(myJson, i => i.Index(index));
            if (response.IsValid)
            {
                return true;
            }
            return false;
        }

        public bool CheckExits(string dataCheck, string index)
        {
            var res = elasticClient.Search<fileInfo>(s => s.Index(index)
           .Query(q => q.Term(p => p.Name.Suffix("keyword"), dataCheck)));

            if (res.Hits.Count > 0)
            {
                return true;
            }
            return false;
        }

        public string GetId(string oldPath, string index)
        {
            var res = elasticClient.Search<fileInfo>(s => s.Index(index)
            .Query(q=>q.Term(p=>p.Path.Suffix("keyword"),oldPath)));


            var id = "";
            if (res.Hits.Count > 0)
            {
                foreach (var hit in res.Hits)
                {
                    id = hit.Id.ToString();
                }
                return id;
            }
            return null;
          

        }
        public bool Update(fileInfo file, string id, string index)
        {
           var res = elasticClient.Update<fileInfo>(id,
                d => d.Index(index)
                .Doc(file));
            if (res.IsValid)
            {
                return true;
            }
            return false;
        }

        public bool Deleted(string id, string index)
        {
            var response = elasticClient.Delete<fileInfo>(id, s => s.Index(index));

            if (response.IsValid)
            {
                Debug.WriteLine("xoa thanh cong ");
                return true;
            }
            return false;
        }

        public List<fileInfo> Search(string text, string index)
        {

            List<fileInfo> myList = new List<fileInfo>();
            var res = elasticClient.Search<fileInfo>(
                s => s
                .Size(200)
                .Index(index)
                .Query(q => q.MultiMatch(m => m.Fields(d => d
                .Field("name")
                .Field("path")
                .Field("content")
                )
                .Query(text)
                .Type(TextQueryType.PhrasePrefix))));

            foreach (var hit in res.Hits)
            {
                IFileInfoBuilder fileInfoBuilder = new InfoBuilder();
                fileInfo fileInfo = fileInfoBuilder.AddName(hit.Source.Name).AddContent(hit.Source.Content).AddPath(hit.Source.Path).Build();
                myList.Add(fileInfo);
            }

            return myList;

        }
    }
}
