using System;
using ToolBelt;
using System.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ServiceBelt
{
    public class ParsedUrlSerializer : SerializerBase<ParsedUrl>
    {
        public ParsedUrlSerializer()
        {
        }

        public override ParsedUrl Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            IBsonReader reader = context.Reader;
            BsonType bsonType = reader.GetCurrentBsonType();

            switch (bsonType)
            {
            case BsonType.Null:
                reader.ReadNull();
                return null;
            case BsonType.String:
                return new ParsedUrl(reader.ReadString());
            default:
                throw base.CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ParsedUrl value)
        {
            context.Writer.WriteString(value.ToString());
        }
    }
}

