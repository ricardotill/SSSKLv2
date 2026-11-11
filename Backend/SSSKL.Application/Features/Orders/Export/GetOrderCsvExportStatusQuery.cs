using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Export;

public sealed record GetOrderCsvExportStatusQuery(Guid JobId) : IRequest<CsvExportJobDto?>;
