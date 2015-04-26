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
using Rql;
using Rql.MongoDB;
using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStackService = global::ServiceStack.Service;
using System.Threading.Tasks;

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

        public async virtual Task<HttpResult> Post(TSmo smo)
        {
            var dmo = smo.CopyAsNew<TDmo>();
            IValidator<TDmo> validator = container.Resolve<IValidator<TDmo>>();

            dmo.Updated = dmo.Created = DateTime.UtcNow;

            BeforeValidation(dmo);

            validator.ValidateAndThrow(dmo);

            var collectionName = MongoUtils.ToCamelCase(typeof(TDmo).Name);

            await Mongo.GetDatabase().GetCollection<TDmo>(collectionName).InsertOneAsync(dmo);

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

        public async virtual Task<PutResponse> Put(TSmo smo)
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
                dmo = await Mongo.GetCollection<TDmo>().Find(d => d.Id == dmoId).FirstOrDefaultAsync();

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

            await Mongo.GetCollection<TDmo>().ReplaceOneAsync(d => d.Id == dmo.Id, dmo);

            AfterUpdate(dmo);

            return new PutResponse(new RqlDateTime(dmo.Updated));
        }

        public async virtual Task<object> Get(TSmoQuery smoQuery)
        {
            var collectionName = MongoUtils.ToCamelCase(typeof(TDmo).Name);
            var collection = Mongo.GetCollection<TDmo>();
            var fieldsCompiler = new FieldSpecToProjectionDefinition();
            var projection = fieldsCompiler.Compile<TDmo>(smoQuery.Fields);
            IAsyncCursor<TDmo> cursor;

            if (!smoQuery.Id.HasValue)
            {
                var filter = new RqlToMongoFilterDefinition().Compile<TDmo>(smoQuery.Where);
                var sort = new SortSpecToSortDefinition().Compile<TDmo>(smoQuery.Sort);
                var limit = (smoQuery.Limit > 1000 ? 1000 : smoQuery.Limit);
                var skip = (smoQuery.Offset < 0 ? (int)(await collection.CountAsync(filter)) + smoQuery.Offset : smoQuery.Offset);

                using (cursor = await collection.Find(filter).Sort(sort).Limit(limit).Skip(skip).Project(projection).ToCursorAsync())
                {
                    var dmoList = await cursor.ToListAsync();
                    var smoList = dmoList.CopyAsNew<List<TSmo>>();

                    #if DEBUG
                    return new ListResponse<TSmo> 
                        { Items = smoList, Count = smoList.Count, Limit = limit, Offset = skip, DbQuery = (filter == null ? "" : filter.ToString()) };
                    #else
                    return new ListResponse<TSmo> 
                        { Items = smoList, Count = smoList.Count, Limit = limit, Offset = skip };
                    #endif
                }
            }
            else
            {
                var id = smoQuery.Id.ToObjectId();
                var doc = await collection.Find(d => d.Id == id).Project(projection).FirstOrDefaultAsync();

                if (doc == null)
                    throw new HttpError(HttpStatusCode.NotFound, String.Format("Resource '{0}' with id '{1}' not found", collectionName, id));

                return doc.CopyAsNew<TSmo>();
            }
        }
    }
}

