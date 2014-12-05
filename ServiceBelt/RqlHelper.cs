using System;
using MongoDB.Bson;
using Rql;
using Rql.MongoDB;
using ServiceBelt;

namespace ServiceBelt
{
    public static class RqlHelper
    {
        public static void AddRqlPropertyCopiers()
        {
            PropertyCopier.AddTypeConverter<ObjectId, RqlId>(fromValue => ((ObjectId)fromValue).ToRqlId());
            PropertyCopier.AddTypeConverter<RqlId, ObjectId>(fromValue => ((RqlId)fromValue).ToObjectId());

            // RqlDateTime/DateTime
            PropertyCopier.AddTypeConverter<DateTime, RqlDateTime>(fromValue => new RqlDateTime((DateTime)fromValue));
            PropertyCopier.AddTypeConverter<RqlDateTime, DateTime>(fromValue => (DateTime)(RqlDateTime)fromValue);
            PropertyCopier.AddTypeConverter<DateTime?, RqlDateTime?>(
                fromValue => fromValue == null ? new Nullable<RqlDateTime>() : new Nullable<RqlDateTime>(new RqlDateTime((DateTime)fromValue)));
            PropertyCopier.AddTypeConverter<RqlDateTime?, DateTime?>(
                fromValue => fromValue == null ? new Nullable<DateTime>() : new Nullable<DateTime>((DateTime)(RqlDateTime)fromValue));

            // RqlTimeSpan/TimeSpan
            PropertyCopier.AddTypeConverter<TimeSpan, RqlTimeSpan>(fromValue => new RqlTimeSpan((TimeSpan)fromValue));
            PropertyCopier.AddTypeConverter<RqlTimeSpan, TimeSpan>(fromValue => (TimeSpan)(RqlTimeSpan)fromValue);
            PropertyCopier.AddTypeConverter<TimeSpan?, RqlTimeSpan?>(
                fromValue => fromValue == null ? new Nullable<RqlTimeSpan>() : new Nullable<RqlTimeSpan>(new RqlTimeSpan((TimeSpan)fromValue)));
            PropertyCopier.AddTypeConverter<RqlTimeSpan?, TimeSpan?>(
                fromValue => fromValue == null ? new Nullable<TimeSpan>() : new Nullable<TimeSpan>((TimeSpan)(RqlTimeSpan)fromValue));

        }
    }
}

