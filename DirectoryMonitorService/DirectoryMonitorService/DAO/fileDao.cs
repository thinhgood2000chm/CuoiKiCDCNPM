﻿using System;
using Nest;
using System.Collections.Generic;
using System.Diagnostics;

namespace DirectoryMonitorService.DAO
{
    class fileDao
    {
        static ConnectionSettings connectionSettings = new ConnectionSettings(new Uri("http://localhost:9200/")); //local PC      
        ElasticClient elasticClient = new ElasticClient(connectionSettings);
        public bool AddList(List<fileInfo> fileInfos)
        {
            var bulkIndexResponse = elasticClient.Bulk(b => b
               .Index("filedatasearch2")
               .IndexMany(fileInfos)
             );

            if (bulkIndexResponse.IsValid)
            {
                return true;
            }
            return false;
        }

        public bool Add(fileInfo file)
        {
            var myJson = new
            {
                name = file.name,
                path = file.path,
                content = file.content
            };
            var response = elasticClient.Index(myJson, i => i.Index("filedatasearch2"));
            if (response.IsValid)
            {
                return true;
            }
            return false;
        }

        public bool CheckExits(string dataCheck)
        {
            var response = elasticClient.Search<fileInfo>(s => s.Index("filedatasearch2")
             .Query(q => q.Match(m => m.Field("name").Query(dataCheck))));
            if (response.Hits.Count > 0)
            {
                return true;
            }
            return false;
        }

        public string GetId(string oldPath)
        {
            var res = elasticClient.Search<fileInfo>(s => s.Index("filedatasearch2")
            .Query(q=>q.Term(p=>p.path.Suffix("keyword"),oldPath)));


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
        public bool Update(fileInfo file, string id)
        {
           var res = elasticClient.Update<fileInfo>(id,
                d => d.Index("filedatasearch2")
                .Doc(file));
            if (res.IsValid)
            {
                return true;
            }
            return false;
        }

        public bool Deleted(string id)
        {
            var response = elasticClient.Delete<fileInfo>(id, s => s.Index("filedatasearch2"));

            if (response.IsValid)
            {
                Debug.WriteLine("xoa thanh cong ");
                return true;
            }
            return false;
        }

        public List<fileInfo> Search(string text)
        {

            List<fileInfo> myList = new List<fileInfo>();
            var res = elasticClient.Search<fileInfo>(
                s => s
                .Size(200)
                .Index("filedatasearch2")
                .Query(q => q.MultiMatch(m => m.Fields(d => d
                .Field("name")
                .Field("path")
                .Field("content")
                )
                .Query(text)
                .Type(TextQueryType.PhrasePrefix))));

            foreach (var hit in res.Hits)
            {
                myList.Add(new fileInfo()
                {
                    name = hit.Source.name,
                    path = hit.Source.path,
                    content = hit.Source.content
                });
            }

            return myList;

        }
    }
}
