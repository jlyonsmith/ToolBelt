using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Rql;
using Rql.MongoDB;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStackService = global::ServiceStack.Service;

namespace ServiceBelt
{
    public class MongoService<TSmo, TSmoQuery, TDmo> 
        : ServiceStackService 
        where TSmo: ResourceBase, new()
        where TSmoQuery: ResourceGetParams
        where TDmo: ICollectionObject, new()
    {
        public MongoService()
        {
            this.container = ServiceStackHost.Instance.Container;
        }

        public MongoService(Funq.Container container)
        {
            this.container = container;
        }

        public IMongoManager Mongo { get; set; }
        private Funq.Container container;

        public virtual void BeforeValidation(TDmo dmo)
        {
        }

        public virtual void AfterUpdate(TDmo dmo)
        {
        }

        public virtual HttpResult Post(TSmo smo)
        {
            var dmo = smo.CopyAsNew<TDmo>();
            IValidator<TDmo> validator = container.Resolve<IValidator<TDmo>>();

            dmo.Updated = dmo.Created = DateTime.UtcNow;

            BeforeValidation(dmo);

            validator.ValidateAndThrow(dmo);

            var collectionName = MongoUtils.ToCamelCase(typeof(TDmo).Name);

            Mongo.GetDatabase().GetCollection<TDmo>(collectionName).Save(dmo);

            AfterUpdate(dmo);

            var smoId = dmo.Id.ToRqlId();
            var result = new HttpResult(new PostResponse(RqlDateTime.UtcNow, smoId), HttpStatusCode.Created);

            if (this.Request != null)
            {
                // Give the canonical location of the new entity
                result.Headers[HttpHeaders.Location] = this.Request.AbsoluteUri + "/" + HttpUtility.UrlEncode(smoId.ToString());
            }

            return result;
        }

        public virtual void Delete(TSmoQuery smoQuery)
        {
            try
            {
                Mongo.Delete(typeof(TDmo), smoQuery.Id.Value.ToObjectId());
            }
            catch (Exception)
            {
                throw new HttpError(HttpStatusCode.NotFound, "{0} with id {1} was not found".Fmt(MongoUtils.ToCamelCase(typeof(TSmo).Name), smoQuery.Id));
            }
        }

        public virtual PutResponse Put(TSmo smo)
        {
            var dmoId = smo.Id.ToObjectId();
            TDmo dmo = default(TDmo);

            if (String.IsNullOrEmpty(smo.Fields))
            {
                // No fields given; just overwrite the entire object
                dmo = smo.CopyAsNew<TDmo>();
            }
            else
            {
                // Fields given; get the current object value and overwrite with just the fields specified
                dmo = Mongo.GetCollection<TDmo>().FindOneById(dmoId);

                if (dmo == null)
                    throw HttpError.NotFound("Resource '{0}' id '{1}' was not found".Fmt(MongoUtils.ToCamelCase(smo.GetType().Name), smo.Id));

                var fieldHash = new FieldSpecParser()
                    .Parse(smo.Fields).Fields
                    .Where(f => f.Presence == FieldSpecPresence.Included)
                    .Select(f => f.Name)
                    .ToHashSet();

                PropertyCopier.Copy(smo, dmo, pi => fieldHash.Contains(MongoUtils.ToCamelCase(pi.Name)));
            }

            // Set new updated time
            dmo.Updated = DateTime.UtcNow;
            
            IValidator<TDmo> validator = container.Resolve<IValidator<TDmo>>();

            BeforeValidation(dmo);

            var result = validator.Validate(dmo);

            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);   
            }

            IMongoUpdate update = Update.Replace(dmo);

            Mongo.GetCollection<TDmo>().Update(Query<TDmo>.EQ(e => e.Id, dmo.Id), update);

            AfterUpdate(dmo);

            return new PutResponse(new RqlDateTime(dmo.Updated));
        }

        public virtual object Get(TSmoQuery smoQuery)
        {
            var collectionName = MongoUtils.ToCamelCase(typeof(TDmo).Name);
            var collection = Mongo.GetCollection<TDmo>();
            var fieldsCompiler = new FieldSpecToMongoFieldsCompiler();
            var fields = (smoQuery.Fields== null ? Fields.Null : fieldsCompiler.Compile(smoQuery.Fields));
            MongoCursor<TDmo> cursor;

            if (!smoQuery.Id.HasValue)
            {
                var queryCompiler = new RqlToMongoQueryCompiler();
                var query = (smoQuery.Where == null ? Query.Null : queryCompiler.Compile(smoQuery.Where));

                var sortByCompiler = new SortSpecToMongoSortByCompiler();
                var sortBy = (smoQuery.Sort== null ? SortBy.Null : sortByCompiler.Compile(smoQuery.Sort));

                if (sortBy == SortBy.Null)
                    sortBy = new SortByBuilder().Ascending(new[] { "$natural" });

                var limit = (smoQuery.Limit > 1000 ? 1000 : smoQuery.Limit);

                var skip = smoQuery.Offset;

                if (skip < 0)
                    skip = (int)collection.Count(query) + skip;

                cursor = collection.Find(query).SetSortOrder(sortBy).SetLimit(limit).SetSkip(skip).SetFields(fields);

                var dmoList = cursor.ToList();
                var smoList = dmoList.CopyAsNew<List<TSmo>>();

                #if DEBUG
                return new ListResponse<TSmo> 
                    { Items = smoList, Count = smoList.Count, Limit = limit, Offset = skip, DbQuery = (query == null ? "" : query.ToString()) };
                #else
                return new ListResponse<TSmo> 
                    { Items = smoList, Count = smoList.Count, Limit = limit, Offset = skip };
                #endif
            }
            else
            {
                var id = smoQuery.Id.ToObjectId();

                cursor = collection.Find(Query<TDmo>.EQ(d => d.Id, id)).SetFields(fields);

                if (cursor.Count() == 0)
                    throw new HttpError(HttpStatusCode.NotFound, String.Format("Resource '{0}' with id '{1}' not found", collectionName, id));

                return cursor.ToList()[0].CopyAsNew<TSmo>();
            }
        }
    }
}

