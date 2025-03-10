using BitcoinPriceAggregator.Application.Common.Models;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FluentValidation;
using System.Linq;

namespace BitcoinPriceAggregator.Api.Controllers
{
    /// <summary>
    /// Controller for managing Bitcoin price operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/bitcoin-price")]
    public class BitcoinPriceController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<BitcoinPriceController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitcoinPriceController"/> class.
        /// </summary>
        /// <param name="mediator">The mediator instance.</param>
        /// <param name="logger">The logger instance.</param>
        public BitcoinPriceController(IMediator mediator, ILogger<BitcoinPriceController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Gets the Bitcoin price for a specific timestamp.
        /// </summary>
        /// <param name="timestamp">The timestamp to get the price for.</param>
        /// <param name="pair">The trading pair (e.g., "BTC/USD").</param>
        /// <returns>The price information.</returns>
        [HttpGet("price/{timestamp}")]
        [ProducesResponseType(typeof(ApiResponse<BitcoinPriceDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetPrice([Required] DateTimeOffset timestamp, string pair = "BTC/USD")
        {
            try
            {
                var query = new GetAggregatedPriceQuery(timestamp.ToUnixTimeMilliseconds(), pair);
                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<BitcoinPriceDto> { Success = true, Data = result });
            }
            catch (FluentValidation.ValidationException ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Errors.First().ErrorMessage });
            }
        }

        /// <summary>
        /// Gets Bitcoin prices within a time range.
        /// </summary>
        /// <param name="startTime">The start time of the range.</param>
        /// <param name="endTime">The end time of the range.</param>
        /// <param name="pair">The trading pair (e.g., "BTC/USD").</param>
        /// <returns>A list of price information within the range.</returns>
        [HttpGet("price-range")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BitcoinPriceDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 500)]
        public async Task<IActionResult> GetPriceRange(
            [Required] DateTimeOffset startTime,
            [Required] DateTimeOffset endTime,
            string pair = "BTC/USD")
        {
            try
            {
                var query = new GetPriceRangeQuery
                {
                    StartTicks = startTime.ToUnixTimeMilliseconds(),
                    EndTicks = endTime.ToUnixTimeMilliseconds(),
                    Pair = pair
                };

                var result = await _mediator.Send(query);
                return Ok(new ApiResponse<IEnumerable<BitcoinPriceDto>> { Success = true, Data = result });
            }
            catch (FluentValidation.ValidationException ex)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Errors.First().ErrorMessage });
            }
        }
    }
}