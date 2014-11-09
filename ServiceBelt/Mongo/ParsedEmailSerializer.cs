using System;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using ToolBelt;
using MongoDB.Bson;
using System.IO;

namespace ServiceBelt
{
    /// <summary>
    /// Represents a serializer for ParsedEmail.
    /// </summary>
    public class ParsedEmailSerializer : BsonBaseSerializer
    {
        // private static fields
        private static ParsedEmailSerializer instance = new ParsedEmailSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the BsonDoubleSerializer class.
        /// </summary>
        public ParsedEmailSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BsonDoubleSerializer class.
        /// </summary>
        public static ParsedEmailSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(ParsedEmail));

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
            case BsonType.Null:
                bsonReader.ReadNull();
                return null;
            case BsonType.String:
                return new ParsedEmail(bsonReader.ReadString());
            default:
                var message = string.Format("Cannot deserialize ParsedEmail from BsonType {0}.", bsonType);
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

