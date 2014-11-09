using System;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using ToolBelt;
using System.IO;

namespace ServiceBelt
{
    /// <summary>
    /// Represents a serializer for ParsedUrl.
    /// </summary>
    public class ParsedUrlSerializer : BsonBaseSerializer
    {
        // private static fields
        private static ParsedUrlSerializer instance = new ParsedUrlSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDoubleSerializer class.
        /// </summary>
        public ParsedUrlSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonDoubleSerializer class.
        /// </summary>
        public static ParsedUrlSerializer Instance
        {
            get { return instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            VerifyTypes(nominalType, actualType, typeof(ParsedUrl));

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
            case BsonType.Null:
                bsonReader.ReadNull();
                return null;
            case BsonType.String:
                return new ParsedUrl(bsonReader.ReadString());
            default:
                var message = string.Format("Cannot deserialize ParsedUrl from BsonType {0}.", bsonType);
                throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var bsonString = new BsonString(value.ToString());
            bsonWriter.WriteString(bsonString.Value);
        }
    }
}

