using System;
using Nest;
using System.Collections.Generic;


namespace fileExplore.Dao
{
    class fileDao
    {
        static ConnectionSettings connectionSettings = new ConnectionSettings(new Uri("http://localhost:9200/")); //local PC      
        ElasticClient elasticClient = new ElasticClient(connectionSettings);
        public bool AddList(List<fileInfo> fileInfos,string index)
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

        public bool Add(fileInfo file, string index) // add này dùng cho nếu có file tạo mới ( chạy ngầm sẽ dùng)
        {
            var myJson = new
            {
                name = file.name,
                path = file.path,
                content = file.content
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
            var response = elasticClient.Search<fileInfo>(s => s.Index(index)
             .Query(q => q.Match(m => m.Field("name").Query(dataCheck))));
            if (response.Hits.Count > 0)
            {
                return true;
            }
            return false;
        }
         
    }
}
