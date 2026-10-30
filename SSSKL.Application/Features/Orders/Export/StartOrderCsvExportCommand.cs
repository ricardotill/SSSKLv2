using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Export;

public sealed record StartOrderCsvExportCommand(string StartedByUserId) : IRequest<StartOrderCsvExportResult>;

/// <param name="Started">False when another export was still running; <paramref name="Job"/> is then that export.</param>
public sealed record StartOrderCsvExportResult(CsvExportJobDto Job, bool Started);
