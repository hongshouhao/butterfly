using AutoMapper;
using Butterfly.Server.Common;
using Butterfly.Server.ViewModels;
using Butterfly.Storage;
using Butterfly.Storage.Query;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Butterfly.Web.Controllers
{
    [Route("api/[controller]")]
    public class StatisticController
    {
        private readonly ISpanQuery _spanQuery;
        private readonly IMapper _mapper;

        public StatisticController(ISpanQuery spanQuery, IMapper mapper)
        {
            _spanQuery = spanQuery;
            _mapper = mapper;
        }

        [HttpGet("Histogram")]
        public async Task<TraceHistogramViewModel> GetTraceHistogram(
            [FromQuery] string service, [FromQuery] string tags,
            [FromQuery] long? startTimestamp, [FromQuery] long? finishTimestamp,
            [FromQuery] int? minDuration, [FromQuery] int? maxDuration, [FromQuery] int? limit)
        {
            var query = new TraceQuery
            {
                Tags = tags,
                ServiceName = service,
                StartTimestamp = TimestampHelpers.Convert(startTimestamp),
                FinishTimestamp = TimestampHelpers.Convert(finishTimestamp),
                MinDuration = minDuration,
                MaxDuration = maxDuration,
                Limit = limit.GetValueOrDefault(10)
            };

            var data = await _spanQuery.GetTraceHistogram(query);
            return null;
            //return _mapper.Map<List<TraceHistogramViewModel>>(data);
        }
    }
}
