using System;
using Nest;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using fileExplore.Dao;

namespace fileExplore.Dao
{
    class fileDao
    {
        static ConnectionSettings connectionSettings = new ConnectionSettings(new Uri("http://localhost:9200/")); //local PC      
        ElasticClient elasticClient = new ElasticClient(connectionSettings);
        public bool Add(List<fileInfo> fileInfos)
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
         
    }
}
