using System;
using ToolBelt;
using System.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ServiceBelt
{
    public class ParsedEmailSerializer : SerializerBase<ParsedEmail>
    {
        public ParsedEmailSerializer()
        {
        }

        public override ParsedEmail Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            IBsonReader reader = context.Reader;
            BsonType bsonType = reader.GetCurrentBsonType();

            switch (bsonType)
            {
            case BsonType.Null:
                reader.ReadNull();
                return null;
            case BsonType.String:
                return new ParsedEmail(reader.ReadString());
            default:
                throw base.CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ParsedEmail value)
        {
            context.Writer.WriteString(value.ToString());
        }
    }
}

