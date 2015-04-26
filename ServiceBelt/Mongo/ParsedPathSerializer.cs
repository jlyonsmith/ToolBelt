using System;
using System.IO;
using ToolBelt;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;

namespace ServiceBelt
{
    public class ParsedPathSerializer : SerializerBase<ParsedPath>
    {
        public ParsedPathSerializer()
        {
        }

        public override ParsedPath Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            IBsonReader reader = context.Reader;
            BsonType bsonType = reader.GetCurrentBsonType();

            switch (bsonType)
            {
            case BsonType.Null:
                reader.ReadNull();
                return null;
            case BsonType.String:
                return new ParsedPath(reader.ReadString(), PathType.Unknown);
            default:
                throw base.CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ParsedPath value)
        {
            context.Writer.WriteString(value.ToString());
        }
    }
}
