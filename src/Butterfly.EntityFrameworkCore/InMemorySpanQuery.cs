﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Butterfly.EntityFrameworkCore.Models;
using Butterfly.DataContract.Tracing;
using Butterfly.Storage;
using Butterfly.Storage.Query;
using Microsoft.EntityFrameworkCore;

namespace Butterfly.EntityFrameworkCore
{
    internal class InMemorySpanQuery : ISpanQuery
    {
        private readonly InMemoryDbContext _dbContext;
        private readonly IMapper _mapper;

        public IQueryable<SpanModel> _spanQuery
        {
            get { return _dbContext.Spans.AsNoTracking().Include(x => x.Baggages).Include(x => x.Tags).Include(x => x.References).Include(x => x.Logs).ThenInclude(x => x.Fields); }
        }

        public InMemorySpanQuery(InMemoryDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public Task<Span> GetSpan(string spanId)
        {
            var span = _mapper.Map<Span>(_spanQuery.FirstOrDefault(x => x.SpanId == spanId));
            return Task.FromResult(span ?? new Span());
        }

        public Task<Trace> GetTrace(string traceId)
        {
            var spans = _dbContext.Spans.AsNoTracking().Include(x => x.Tags).Include(x => x.References).Where(x => x.TraceId == traceId).OrderBy(x => x.StartTimestamp).ToList();
            var result = new Trace
            {
                TraceId = traceId,
                Spans = _mapper.Map<List<Span>>(spans)
            };
            return Task.FromResult(result);
        }

        public Task<IEnumerable<Trace>> GetTraces(TraceQuery traceQuery)
        {
            var query = _dbContext.Spans.Include(x => x.Tags).OrderByDescending(x => x.StartTimestamp).AsQueryable();

            if (traceQuery.StartTimestamp != null)
            {
                query = query.Where(x => x.StartTimestamp >= traceQuery.StartTimestamp);
            }

            if (traceQuery.FinishTimestamp != null)
            {
                query = query.Where(x => x.FinishTimestamp <= traceQuery.FinishTimestamp);
            }

            var queryTags = BuildQueryTags(traceQuery).ToList();
            if (queryTags.Any())
            {
                var traceIdsQuery = query;

                foreach (var item in queryTags)
                {
                    var tag = item;
                    traceIdsQuery = traceIdsQuery.Where(x => x.Tags.Any(t => t.Key == tag.Key && t.Value == tag.Value));
                }

                var traceIds = traceIdsQuery.Select(x => x.TraceId).Distinct().ToList();

                query = query.Where(x => traceIds.Contains(x.TraceId));
            }

            var queryGroup = query.ToList().GroupBy(x => x.TraceId).Take(traceQuery.Limit).ToList();

            return Task.FromResult<IEnumerable<Trace>>(queryGroup.Select(x => new Trace() { TraceId = x.Key, Spans = _mapper.Map<List<Span>>(x.ToList()) }).ToList());
        }

        public Task<IEnumerable<Span>> GetSpanDependencies(DependencyQuery dependencyQuery)
        {
            var query = _dbContext.Spans.AsNoTracking().Include(x => x.References).Include(x => x.Tags).AsQueryable();

            if (dependencyQuery.StartTimestamp != null)
            {
                query = query.Where(x => x.StartTimestamp >= dependencyQuery.StartTimestamp);
            }

            if (dependencyQuery.FinishTimestamp != null)
            {
                query = query.Where(x => x.FinishTimestamp <= dependencyQuery.FinishTimestamp);
            }

            return Task.FromResult(_mapper.Map<IEnumerable<Span>>(query.ToList()));
        }

        private IEnumerable<Tag> BuildQueryTags(TraceQuery traceQuery)
        {
            if (!string.IsNullOrEmpty(traceQuery.ServiceName))
            {
                yield return new Tag { Key = QueryConstants.Service, Value = traceQuery.ServiceName };
            }

            if (!string.IsNullOrEmpty(traceQuery.Tags))
            {
                var tags = traceQuery.Tags.Split('|');
                foreach (var tag in tags)
                {
                    var pair = tag.Split('=');
                    if (pair.Length == 2)
                    {
                        yield return new Tag { Key = pair[0], Value = pair[1] };
                    }
                }
            }
        }

        public Task<IEnumerable<TraceHistogram>> GetTraceHistogram(TraceQuery traceQuery)
        {
            traceQuery.Ensure();

            var query = _dbContext.Spans.AsQueryable();

            if (traceQuery.StartTimestamp != null)
            {
                query = query.Where(x => x.StartTimestamp >= traceQuery.StartTimestamp);
            }

            if (traceQuery.FinishTimestamp != null)
            {
                query = query.Where(x => x.FinishTimestamp <= traceQuery.FinishTimestamp);
            }

            var queryGroup = query.ToList().GroupBy(x => x.TraceId).ToList();

            var histogram = queryGroup.GroupBy(x => x.Min(s => s.StartTimestamp).ToString("yyyy-MM-dd HH:mm")).Select(x => new TraceHistogram { Count = x.Count(), Time = DateTimeOffset.Parse(x.Key) });
            return Task.FromResult<IEnumerable<TraceHistogram>>(histogram.ToList());
        }

        public Task<IEnumerable<TraceOperationHistogram>> GetSpanHistogramByOperaionName(TraceQuery traceQuery)
        {
            throw new NotImplementedException();
        }
    }
}