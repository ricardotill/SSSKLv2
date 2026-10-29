using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Export;

public sealed record StartOrderCsvExportCommand(string StartedByUserId) : IRequest<StartOrderCsvExportResult>;

/// <param name="Started">False when another export was still running; <paramref name="Job"/> is then that export.</param>
public sealed record StartOrderCsvExportResult(CsvExportJobDto Job, bool Started);

internal sealed class StartOrderCsvExportHandler(OrderCsvExportJob exportJob)
    : IRequestHandler<StartOrderCsvExportCommand, StartOrderCsvExportResult>
{
    public Task<StartOrderCsvExportResult> Handle(StartOrderCsvExportCommand request, CancellationToken cancellationToken)
    {
        var (job, started) = exportJob.StartExport(request.StartedByUserId);
        return Task.FromResult(new StartOrderCsvExportResult(job, started));
    }
}
