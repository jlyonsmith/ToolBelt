using NUnit.Framework;
using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;

namespace ToolBelt.MongoDB.Tests
{
	class Data
	{
		public ParsedPath Path { get; set; }
	}
	
    [TestFixture()]
    public class BsonParsedPathTests
    {
    	[TestFixtureSetUp]
		public void TestFixtureSetup()
		{
			BsonSerializer.RegisterSerializer(typeof(ParsedPath), new ParsedPathSerializer());
		}
    
        [Test()]
        public void TestSerializeDeserialize()
        {
        	var path = "c:/a/b/c/f.txt";
			var data = new Data 
			{
				Path = new ParsedPath(path, PathType.File)
			};
			
			var doc = new BsonDocument();
			
			using (var writer = new BsonDocumentWriter(doc, new BsonDocumentWriterSettings()))
			{
        		BsonSerializer.Serialize(writer, data);
        	}
        	
			Assert.AreEqual(doc["Path"], new BsonString(path));
			
			using (var reader = new BsonDocumentReader(doc, new BsonDocumentReaderSettings()))
			{
				data = BsonSerializer.Deserialize<Data>(doc);
			}
			
			Assert.AreEqual(data.Path, new ParsedPath(path, PathType.File));
        }
    }
}

