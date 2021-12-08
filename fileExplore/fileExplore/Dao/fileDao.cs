using System;
using Nest;
using System.Collections.Generic;


namespace fileExplore.Dao
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
             .Query(q => q.Match(m => m.Field("name").Query("test2"))));
            if (response.Hits.Count > 0)
            {
                return true;
            }
            return false;
        }
         
    }
}
