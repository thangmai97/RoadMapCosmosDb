using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosPresent.Models.Container
{
    public class Audit
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Type { get; set; }
        public Document Document { get; set; }
        public File[] FileType { get; set; }
        public DateTime DateCreated { get; set; }
        public string Tenant { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        public virtual string PartitionKey{get;set;}
        public static string GetPartitionKey(string tenant,DateTime DateCreated)
        {
            return $"{tenant}-{DateCreated.ToString("yyyy-MM") }";
        }
    }

    public class Document
    {
        public string Name { get; set; }
    }

    public class File
    {
        public string Name { get; set; }
        public FileType Type { get; set; }
    }
    public enum FileType
    {
        CSV,
        PDF
    }

   
}
